using Microsoft.AspNetCore.Components;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS;
using Anaglyphohol.MultiviewMaker.Renderers;

namespace Anaglyphohol.MultiviewMaker
{
    public class AnaglyphImageMakerService : IDisposable
    {
        /// <summary>
        /// Fires when the progress of the processing changes<br/>
        /// The bool value indicates if the processing is still ongoing
        /// </summary>

        public event Action<bool> ProgressChanged = default!;
        public event Action OnStateHasChanged = default!;

        DepthEstimationService DepthEstimationService;

        BlazorJSRuntime JS;

        int _AnaglyphProfile = 0;
        public int AnaglyphProfile
        {
            get => _AnaglyphProfile;
            set
            {
                if (_AnaglyphProfile != value)
                {
                    _AnaglyphProfile = value;
                    OnStateHasChanged?.Invoke();
                }
            }
        }

        float _Focus3D = 0.5f;
        public float Focus3D
        {
            get => _Focus3D;
            set
            {
                if (_Focus3D != value)
                {
                    _Focus3D = value;
                    OnStateHasChanged?.Invoke();
                }
            }
        }

        float _Level3D = 1.0f;
        public float Level3D
        {
            get => _Level3D;
            set
            {
                if (_Level3D != value)
                {
                    _Level3D = value;
                    OnStateHasChanged?.Invoke();
                }
            }
        }

        public AnaglyphRenderer? anaglyphRenderer { get; private set; }
        public bool Processing { get; set; }
        public bool ProcessingFailed { get; set; }

        public AnaglyphImageMakerService(BlazorJSRuntime js, DepthEstimationService depthEstimationService)
        {
            JS = js;
            DepthEstimationService = depthEstimationService;
            anaglyphRenderer = new AnaglyphRenderer();
        }

        public async Task<bool> DownloadImage(string filename, float? quality = null)
        {
            if (anaglyphRenderer == null) return false;
            var ext = filename.Split(".").Last().ToLowerInvariant();
            string? mimeType = null;
            switch (ext)
            {
                case "jpg":
                case "jpeg":
                    mimeType = "image/jpeg";
                    break;
                default:
                    mimeType = $"image/{ext}";
                    break;
            }
            var objectUrl = await anaglyphRenderer.ToObjectUrl(mimeType, quality);
            if (string.IsNullOrEmpty(objectUrl)) return false;
            DownloadFile(objectUrl, filename);
            URL.RevokeObjectURL(objectUrl);
            return true;
        }
        public async Task<string?> GetImageObjectUrl(string? mimeType = null, float? quality = null)
        {
            if (anaglyphRenderer == null) return null;
            if (string.IsNullOrEmpty(mimeType))
            {
                mimeType = "image/png";
            }
            var objectUrl = await anaglyphRenderer.ToObjectUrl(mimeType, quality);
            return objectUrl;
        }
        void DownloadFile(string url, string filename)
        {
            using var document = JS.GetDocument();
            using var a = document!.CreateElement<HTMLAnchorElement>("a");
            a.Href = url;
            a.Download = filename;
            document.Body!.AppendChild(a);
            a.Click();
            document.Body.RemoveChild(a);
        }
        /// <summary>
        /// bool parameter is true if the render was successfull
        /// </summary>
        public event Action<bool> RenderComplete = default!;
        public bool ProcessingSuccess { get; private set; }

        SemaphoreSlim renderLock = new SemaphoreSlim(1, 1); 
        public async Task<string?> Update(HTMLImageElement image, float? level3D = null, float? focus3D = null, int? anaglyphProfile = null)
        {
            if (anaglyphRenderer == null) return null;
            try
            {
                await renderLock.WaitAsync();
                ProcessingFailed = false;
                Processing = true;
                ProgressChanged?.Invoke(true);
                OnStateHasChanged?.Invoke();
                var imageWithDepth = await DepthEstimationService.ImageTo2DZImage(image);
                if (imageWithDepth == null)
                {
                    throw new Exception("Returned depth map was null");
                }
                anaglyphRenderer.Level3D = level3D ?? Level3D;
                anaglyphRenderer.Focus3D = focus3D ?? Focus3D;
                anaglyphRenderer.ProfileIndex = anaglyphProfile ?? AnaglyphProfile;
                anaglyphRenderer.SetInput(imageWithDepth, "2dz");
                anaglyphRenderer.Render();
                var objectUrl = await anaglyphRenderer.ToObjectUrl("image/png");
                ProcessingSuccess = true;
                return objectUrl;
            }
            catch (Exception ex)
            {
                ProcessingSuccess = false;
                ProcessingFailed = true;
                //JS.Log("Depthmap creation failed >>", ex.Message);
            }
            finally
            {
                Processing = false;
                ProgressChanged?.Invoke(false);
                OnStateHasChanged?.Invoke();
                RenderComplete?.Invoke(ProcessingSuccess);
                renderLock.Release();
            }
            return null;
        }
        public async Task<bool> Update(HTMLImageElement image, HTMLCanvasElement renderTarget)
        {
            if (anaglyphRenderer == null) return false;
            try
            {
                await renderLock.WaitAsync();
                //JS.Log("Update >>>>>>>>>>", image, renderTarget);
                //JS.Set("_image", image);
                //JS.Set("_renderTarget", renderTarget);
                ProcessingFailed = false;
                Processing = true;
                ProgressChanged?.Invoke(true);
                var imageWithDepth = await DepthEstimationService.ImageTo2DZImage(image);
                if (imageWithDepth == null)
                {
                    throw new Exception("Returned depth map was null");
                }
                anaglyphRenderer.Level3D = Level3D;
                anaglyphRenderer.Focus3D = Focus3D;
                anaglyphRenderer.ProfileIndex = AnaglyphProfile;
                anaglyphRenderer.SetInput(imageWithDepth, "2dz");
                anaglyphRenderer.Render();
                // resize target
                renderTarget.Width = image.NaturalWidth;
                renderTarget.Height = image.NaturalHeight;
                // draw to target
                using var ctx = renderTarget.Get2DContext();
                ctx.DrawImage(anaglyphRenderer.OffscreenCanvas!, 0, 0, renderTarget.Width, renderTarget.Height);
                ProcessingSuccess = true;
                return true;
            }
            catch (Exception ex)
            {
                ProcessingSuccess = false;
                ProcessingFailed = true;
#if DEBUG
                JS.Log("Depthmap creation failed >>", ex.Message);
#endif
            }
            finally
            {
                Processing = false;
                ProgressChanged?.Invoke(false);
                RenderComplete?.Invoke(ProcessingSuccess);
                renderLock.Release();
            }
            return false;
        }
        /// <inheritdoc/>
        public void Dispose()
        {
            if (anaglyphRenderer != null)
            {
                anaglyphRenderer.Dispose();
                anaglyphRenderer = null;
            }
        }
    }
}
