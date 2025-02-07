using Anaglyphohol.Services;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.BrowserExtension.Services;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.TransformersJS;

namespace Anaglyphohol.MultiviewMaker
{
    /// <summary>
    /// This class handles the loading and caching of depth estimation pipelines.<br/>
    /// It also has methods for creating and caching 2D+Z images from 2D images<br/>
    /// It provides events indicating the progress of loading models
    /// </summary>
    public class DepthEstimationService
    {
        /// <summary>
        /// The progress percentage from 0 to 100
        /// </summary>
        public float? OverallLoadProgress
        {
            get
            {
                var total = (float)ModelProgresses.Values.Sum(p => p.Total ?? 0);
                if (total == 0f) return null;
                var loaded = (float)ModelProgresses.Values.Sum(p => p.Loaded ?? 0);
                return loaded * 100f / total;
            }
        }
        /// <summary>
        /// True if loading models
        /// </summary>
        public bool Loading { get; private set; }
        /// <summary>
        /// True if loading models
        /// </summary>
        public bool ModelsLoaded => DepthEstimationPipelines.Any();
        /// <summary>
        /// Holds the loading progress for models that are loading
        /// </summary>
        public Dictionary<string, ModelLoadProgress> ModelProgresses { get; } = new();
        /// <summary>
        /// Holds all loaded depth estimation pipelines
        /// </summary>
        public Dictionary<string, DepthEstimationPipeline> DepthEstimationPipelines { get; } = new Dictionary<string, DepthEstimationPipeline>();
        /// <summary>
        /// The default depth estimation model. Used if no model is specified.
        /// </summary>
        public string DefaultDepthEstimationModel { get; set; } = "onnx-community/depth-anything-v2-small";
        /// <summary>
        /// Result from a WebGPU support check
        /// </summary>
        public bool WebGPUSupported { get; private set; }
        /// <summary>
        /// IF true, WebGPU will be used (if WebGPUSupported)
        /// </summary>
        public bool UseWebGPU { get; private set; } = true;
        /// <summary>
        /// Cache for generated 2DZ images keyed by the image source string
        /// </summary>
        public Dictionary<string, HTMLImageElement> Cached2DZImages { get; } = new Dictionary<string, HTMLImageElement>();
        BlazorJSRuntime JS;
        Transformers? Transformers = null;
        BrowserExtensionService BrowserExtensionService;
        /// <summary>
        /// The location of the models remote files<br/>
        /// Transformers.env.remoteHost
        /// </summary>
        public string RemoteModelsUrl { get; private set; }
        /// <summary>
        /// The location of the models remote wasm files<br/>
        /// Transformers.env.backends.onnx.wasm.wasmPaths
        /// </summary>
        public string RemoteWasmsUrl { get; private set; }
        /// <summary>
        /// If true, the browser cache will be used to store the models<br/>
        /// </summary>
        public bool UseBrowserCache { get; private set; } = false;
        AppService AppService;
        public DepthEstimationService(BlazorJSRuntime js, BrowserExtensionService browserExtensionService, AppService appService)
        {
            AppService = appService;
            BrowserExtensionService = browserExtensionService;
            JS = js;
            WebGPUSupported = !JS.IsUndefined("navigator.gpu?.requestAdapter");
            RemoteModelsUrl = BrowserExtensionService.GetURL("models/");
            RemoteWasmsUrl = BrowserExtensionService.GetURL("backends/onnx/wasm/");
        }
        public event Action OnStateChange = default!;
        void StateHasChanged()
        {
            OnStateChange?.Invoke();
        }
        void Pipeline_OnProgress(ModelLoadProgress obj)
        {
            if (!string.IsNullOrEmpty(obj.File))
            {
                if (ModelProgresses.TryGetValue(obj.File, out var progress))
                {
                    progress.Status = obj.Status;
                    if (obj.Progress != null) progress.Progress = obj.Progress;
                    if (obj.Total != null) progress.Total = obj.Total;
                    if (obj.Loaded != null) progress.Loaded = obj.Loaded;
                }
                else
                {
                    ModelProgresses[obj.File] = obj;
                }
            }
            StateHasChanged();
        }
        SemaphoreSlim LoadLimiter = new SemaphoreSlim(1);
        public async Task<DepthEstimationPipeline> GetDepthEstimationPipeline()
        {
            var useWebGPU = WebGPUSupported && UseWebGPU;
            var model = DefaultDepthEstimationModel;
            var key = $"{DefaultDepthEstimationModel}+{useWebGPU}";
            if (DepthEstimationPipelines.TryGetValue(key, out var depthEstimationPipeline))
            {
                return depthEstimationPipeline;
            }
            await LoadLimiter.WaitAsync();
            try
            {
                if (DepthEstimationPipelines.TryGetValue(key, out depthEstimationPipeline))
                {
                    return depthEstimationPipeline;
                }
                using var OnProgress = new ActionCallback<ModelLoadProgress>(Pipeline_OnProgress);
                Loading = true;
                if (Transformers == null)
                {
                    Transformers = await Transformers.Init();
                    // Set Transformers environment variables
                    // use the models included with the extension
                    if (!string.IsNullOrEmpty(RemoteModelsUrl))
                    {
                        Transformers.JSRef!.Set("env.remoteHost", RemoteModelsUrl);
                    }
                    if (!string.IsNullOrEmpty(RemoteWasmsUrl))
                    {
                        Transformers.JSRef!.Set("env.backends.onnx.wasm.wasmPaths", RemoteWasmsUrl);
                    }
                    // if browser cache is used, the models will be copied to the cache for every domain they are used on.
                    // they already exist locally in the extension so disable the browser cache
                    Transformers.JSRef!.Set("env.useBrowserCache", UseBrowserCache);
                }
                // Load Depth Estimation Pipeline
                depthEstimationPipeline = await Transformers.DepthEstimationPipeline(model, new PipelineOptions
                {
                    Device = useWebGPU ? "webgpu" : null,
                    OnProgress = OnProgress,
                    Dtype = "fp32",
                });
                DepthEstimationPipelines[key] = depthEstimationPipeline;
                return depthEstimationPipeline;
            }
            finally
            {
                Loading = false;
                ModelProgresses.Clear();
                StateHasChanged();
                LoadLimiter.Release();
            }
        }
        SemaphoreSlim ImageTo2DZImageLimiter = new SemaphoreSlim(1);
        public async Task<HTMLImageElement> ImageTo2DZImage(HTMLImageElement image)
        {
            if (image == null || !image.Complete || image.NaturalWidth == 0 || image.NaturalHeight == 0 || string.IsNullOrEmpty(image.Src))
            {
                throw new Exception("Invalid image");
            }
            var source = image.Src;
            if (Cached2DZImages.TryGetValue(source, out var imageWithDepth))
            {
                return imageWithDepth;
            }
            if (!image.IsImageUsable())
            {
                // try using an image we load ourselves using crossOrigin = "anonymous"
                var altImage = await HTMLImageElement.CreateFromImageAsync(source, "anonymous");
                if (!altImage.IsImageUsable() || altImage.NaturalWidth != image.NaturalWidth || altImage.NaturalHeight != image.NaturalHeight)
                {
#if DEBUG && false
                    JS.Log($"DES: anon failed", image.Src);
#endif
                    altImage = await HTMLImageElement.CreateFromImageAsync(source, "user-credentials");
                    if (!altImage.IsImageUsable() || altImage.NaturalWidth != image.NaturalWidth || altImage.NaturalHeight != image.NaturalHeight)
                    {
#if DEBUG && false
                        JS.Log($"DES: cred failed", image.Src);
#endif
                        throw new Exception("Image cannot be used");
                    }
#if DEBUG && false
                    JS.Log($"DES: cred worked", image.Src);
#endif
                    // successfully loaded image
                    image = altImage;
                }
                else
                {
#if DEBUG && false
                    JS.Log($"DES: anon worked", image.Src);
#endif
                    // successfully loaded image
                    image = altImage;
                }
            }
            try
            {
                await ImageTo2DZImageLimiter.WaitAsync();
                if (Cached2DZImages.TryGetValue(source, out imageWithDepth))
                {
                    return imageWithDepth;
                }
                // get the depth estimation pipeline
                var DepthEstimationPipeline = await GetDepthEstimationPipeline();
                // generate the depth map
                //JS.Log(">> Creating image depthmap");
                // create a RawImage from the HTMLImageElement so the image does not have to be redownloaded
                // this will throw an exception if the image is tainted
                using var rawImage = RawImage.FromImage(image);
                using var depthResult = await DepthEstimationPipeline!.Call(rawImage);
                //JS.Log("<< Depthmap image created");
                using var depthInfo = depthResult.Depth;
                using var depthMapData = depthInfo.Data;
                var depthWidth = depthInfo.Width;
                var depthHeight = depthInfo.Height;
                //Console.WriteLine("Depthmap size: " + depthWidth + "x" + depthHeight);
                // create 2D+Z image object url
                var imageWithDepthObjectUrl = await Create2DZObjectUrl(image, depthMapData, depthWidth, depthHeight);
                imageWithDepth = await HTMLImageElement.CreateFromImageAsync(imageWithDepthObjectUrl);
            }
            catch (Exception ex)
            {
                JS.Log("Depthmap image creation failed", ex);
            }
            finally
            {
                ImageTo2DZImageLimiter.Release();
            }
            Cached2DZImages[source] = imageWithDepth;
            return imageWithDepth;
        }
        async Task<string> Create2DZObjectUrl(HTMLImageElement rgbImage, Uint8Array grayscale1BPPUint8Array, int width, int height)
        {
            var outWidth = width * 2;
            var outHeight = height;
            var grayscale1BPPBytes = grayscale1BPPUint8Array.ReadBytes();
            var depthmapRGBABytes = Grayscale1BPPToRGBA(grayscale1BPPBytes, width, height);
            using var canvas = new HTMLCanvasElement(outWidth, outHeight);
            using var ctx = canvas.Get2DContext();
            // draw rgb image
            ctx.DrawImage(rgbImage);
            // draw depth map
            ctx.PutImageBytes(depthmapRGBABytes, width, height, width, 0);
            using var blob = await canvas.ToBlobAsync("image/png");
            var ret = URL.CreateObjectURL(blob);
            return ret;
        }
        async Task<string> CreateDepthImageObjectUrl(Uint8Array grayscale1BPPUint8Array, int width, int height)
        {
            var grayscale1BPPBytes = grayscale1BPPUint8Array.ReadBytes();
            var depthmapRGBABytes = Grayscale1BPPToRGBA(grayscale1BPPBytes, width, height);
            using var canvas = new HTMLCanvasElement(width, height);
            using var ctx = canvas.Get2DContext();
            ctx.PutImageBytes(depthmapRGBABytes, width, height);
            using var blob = await canvas.ToBlobAsync("image/png");
            var ret = URL.CreateObjectURL(blob);
            return ret;
        }
        byte[] Grayscale1BPPToRGBA(byte[] grayscaleData, int width, int height)
        {
            var ret = new byte[width * height * 4];
            for (var i = 0; i < grayscaleData.Length; i++)
            {
                var grayValue = grayscaleData[i];
                ret[i * 4] = grayValue;     // Red
                ret[i * 4 + 1] = grayValue; // Green
                ret[i * 4 + 2] = grayValue; // Blue
                ret[i * 4 + 3] = 255;       // Alpha
            }
            return ret;
        }
    }
}
