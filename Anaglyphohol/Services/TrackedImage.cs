using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.BrowserExtension.Services;
using SpawnDev.BlazorJS.JSObjects;

namespace Anaglyphohol.Services
{
    public class TrackedImage : IDisposable
    {
        public int AnaglyphProfileId { get; set; }
        public float Level3D { get; set; }
        public float Focus3D{ get; set; }

        public const string ElementUIDKey = "__extensionElementId";
        public const string DoNotTrackElementKey = "__doNotTrackElement";
        public static string? GetElementUID(HTMLElement imageElement) => imageElement.JSRef!.Get<string?>(ElementUIDKey);
        public static bool? GetElementDoNotTrack(HTMLElement imageElement) => imageElement.JSRef!.Get<bool?>(DoNotTrackElementKey);
        public static void SetElementDoNotTrack(HTMLElement imageElement, bool value) => imageElement.JSRef!.Set(DoNotTrackElementKey, value);
        public static void RemoveElementDoNotTrack(HTMLElement imageElement) => imageElement.JSRef!.Delete(DoNotTrackElementKey);
        public static void SetElementUID(HTMLElement imageElement, string videoId) => imageElement.JSRef!.Set(ElementUIDKey, videoId);
        public string UID { get; private set; }
        BlazorJSRuntime JS;
        public HTMLElement Element { get; private set; }
        public HTMLImageElement ImageElement { get; private set; }
        CSSStyleDeclaration? OverlayStyle { get; set; }
        public int MinWidth { get; set; } = 100;
        public int MinHeight { get; set; } = 100;

        /// <summary>
        /// Returns true if the image loading is complete and it meets the minimum size requirements
        /// </summary>
        public bool? MeetsMinSizeRequirements => ImageElement?.Complete != true ? null : ImageElement.Width >= MinWidth && ImageElement.Height >= MinHeight;
        public bool IsImageLoaded => ImageElement?.Complete == true && ImageElement.Width >= 0 && ImageElement.Height >= 0;
        public bool IsHTMLDivElement => Element is HTMLDivElement; // Element.TagName == "DIV";
        public bool IsHTMLImageElement => Element is HTMLImageElement; // Element.TagName == "IMG";
        public TrackedImage(string videoId, HTMLElement imageElement, BlazorJSRuntime js)
        {
            UID = videoId;
            JS = js;
            Element = imageElement;
            OverlayStyle = Element.Style;
            if (imageElement is HTMLImageElement image)
            {
                ImageElement = image;
            }
            else
            {
                ImageElement = new HTMLImageElement();
                using var imageStyle = ImageElement.Style;
                var backgroundImage = GetBackgroundUrl();
                JS.Log("BackgroundImage: " + backgroundImage);
                if (!string.IsNullOrEmpty(backgroundImage))
                {
                    OriginalSource = backgroundImage;
                    ImageElement.Src = backgroundImage;
                }
                using var document = JS.GetDocument();
                using var body = document!.Body!;
                body.AppendChild(ImageElement);
                imageStyle.SetProperty("display", "none");
            }
            AttachImageElementEvents();
            var currentSrc = ImageElement.Src;
            if (!HasOverrideTag(currentSrc))
            {
                OriginalSource = currentSrc;
            }
            else
            {
                OverrideSrc = currentSrc;
            }
        }
        public const string StateAttributeName = "anaglyphohol-state";
        string State = "";
        public void SetState(string state)
        {
            if (State == state) return;
            State = state;
            Element.SetAttribute(StateAttributeName, state);
        }
        public void ShowState(bool show)
        {

        }
        public async Task<bool> RestoreOriginal()
        {
            if (string.IsNullOrEmpty(OriginalSource) || !IsImageLoaded)
            {
                return false;
            }
            return await SetSource(OriginalSource);
        }
        async Task<bool> SetSource(string src)
        {
            if (ImageElement == null || ImageElement.Src == src) return true;
            var tcs = new TaskCompletionSource<bool>();
            var onError = new Action(() => tcs.SetResult(false));
            var onLoad = new Action(() => tcs.SetResult(true));
            ImageElement.OnError += onError;
            ImageElement.OnLoad += onLoad;
            ImageElement.Src = src;
            var succ = await tcs.Task;
            ImageElement.OnError -= onError;
            ImageElement.OnLoad -= onLoad;
            return succ;
        }
        public List<string> UsedOverrides { get; } = new List<string>();
        string? _OverrideSrc = null;
        public string? OverrideSrc
        {
            get => _OverrideSrc;
            set
            {
                var currentSrc = ImageElement.Src;
                if (!HasOverrideTag(currentSrc))
                {
                    OriginalSource = currentSrc;
                }
                if (!string.IsNullOrEmpty(value))
                {
                    value = AddOverrideTag(value);
                }
                if (_OverrideSrc == value)
                {
                    return;
                }
                _OverrideSrc = value;
                if (UseOverride && !string.IsNullOrEmpty(_OverrideSrc))
                {
                    ImageElement.Src = _OverrideSrc;
                }
                else if (currentSrc != OriginalSource && !string.IsNullOrEmpty(OriginalSource))
                {
                    ImageElement.Src = OriginalSource;
                }
            }
        }
        public bool IsOverrideSrc => ImageElement != null && HasOverrideTag(ImageElement.Src);
        bool _UseOverride = true;
        public bool UseOverride
        {
            get => _UseOverride;
            set
            {
                if (_UseOverride == value) return;
                _UseOverride = value;
                var currentSrc = ImageElement.Src;
                if (_UseOverride && !string.IsNullOrEmpty(OverrideSrc))
                {
                    if (currentSrc != OverrideSrc)
                    {
                        ImageElement.Src = OverrideSrc;
                    }
                }
                else if (!string.IsNullOrEmpty(OriginalSource))
                {
                    if (currentSrc != OriginalSource)
                    {
                        ImageElement.Src = OriginalSource;
                    }
                }
            }
        }
        public TrackedImage(HTMLElement imageElement, BlazorJSRuntime js)
        {
            JS = js;
            Element = imageElement;
            UID = GetElementUID(imageElement) ?? "";
            if (string.IsNullOrEmpty(UID))
            {
                UID = Guid.NewGuid().ToString();
                SetElementUID(imageElement, UID);
            }
            if (imageElement is HTMLImageElement image)
            {
                ImageElement = image;
            }
            else
            {
                ImageElement = new HTMLImageElement();
                using var imageStyle = ImageElement.Style;
                var backgroundImage = GetBackgroundUrl();
                JS.Log("BackgroundImage: " + backgroundImage);
                if (!string.IsNullOrEmpty(backgroundImage))
                {
                    OriginalSource = backgroundImage;
                    ImageElement.Src = backgroundImage;
                }
                using var document = JS.GetDocument();
                using var body = document!.Body!;
                body.AppendChild(ImageElement);
                imageStyle.SetProperty("display", "none");
            }
            AttachImageElementEvents();
        }
        string? GetBackgroundUrl()
        {
            using var style = Element.Style;
            var backgroundImage = style?.GetPropertyValue("background-image");
            if (string.IsNullOrEmpty(backgroundImage)) return null;
            if (backgroundImage.StartsWith("url(\""))
            {
                backgroundImage = backgroundImage.Substring(5, backgroundImage.Length - 7);
            }
            else if (backgroundImage.StartsWith("url('"))
            {
                backgroundImage = backgroundImage.Substring(5, backgroundImage.Length - 7);
            }
            else if (backgroundImage.StartsWith("url("))
            {
                backgroundImage = backgroundImage.Substring(4, backgroundImage.Length - 5);
            }
            return backgroundImage;
        }
        string? OriginalSource { get; set; }
        //string AnaglyphImageUrl { get; set; } = "";
        bool AutoHideOverlay = false;
        bool UseOverlay = false;
        void CreateOverlay()
        {
            if (OverlayElement != null) return;
            if (!UseOverlay) return;
            using var parent = Element.ParentElement;
            overlayVisible = false;
            using var document = JS.GetDocument();
            using var body = document!.Body!;

            // 0 size div that contains the overlay div.
            // that way is does not affect the layout of the page
            OverlayContainerElement = document!.CreateElement<HTMLElement>("div");
            using var containerStyle = OverlayContainerElement.Style;
            containerStyle.SetProperty("width", "0");
            containerStyle.SetProperty("height", "0");
            containerStyle.SetProperty("overflow", "visible");
            containerStyle.SetProperty("position", "relative");
            parent!.InsertBefore(OverlayContainerElement, Element);

            OverlayElement = document!.CreateElement<HTMLElement>("div");
            OverlayStyle = OverlayElement.Style;

            OverlayContainerElement.AppendChild(OverlayElement);
            OverlayStyle.SetProperty("position", "absolute");
            //OverlayStyle.SetProperty("width", "20px");
            //OverlayStyle.SetProperty("height", "20px");

            var rect = Element.GetBoundingClientRect();

            OverlayStyle.SetProperty("width", $"{rect.Width}px");
            OverlayStyle.SetProperty("height", $"{rect.Height}px");
            OverlayStyle.SetProperty("border", "1px solid black");
            OverlayStyle.SetProperty("background-color", "red");
            OverlayStyle.SetProperty("z-index", "9999999");
            // OverlayImageElement

            //OverlayElement.AppendChild(anaglyphImage.Canvas!);

            OverlayCanvasElement = document!.CreateElement<HTMLCanvasElement>("canvas");
            // make sure it doesn't get tracked (it's ours)
            //TrackedImage.SetElementDoNotTrack(OverlayImageElement, true);
            //
            OverlayElement.AppendChild(OverlayCanvasElement);

            using var OverlayCanvasElementStyle = OverlayCanvasElement.Style;
            OverlayCanvasElementStyle.SetProperty("width", $"{rect.Width}px");
            OverlayCanvasElementStyle.SetProperty("height", $"{rect.Height}px");
            OverlayCanvasElementStyle.SetProperty("left", "0");
            OverlayCanvasElementStyle.SetProperty("top", "0");

            OverlayStyle.SetProperty("background-color", "green");
            // hide property
            if (AutoHideOverlay)
            {
                HideOverlay();
            }
            HideOverlay();


        }

        private void OverlayTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {

        }
        void AttachImageElementEvents()
        {
            Element.OnLoad += ImageElement_OnLoad;
            ////Element.OnClick += ImageElement_OnClick;
            //Element.OnMouseEnter += ImageElement_OnMouseEnter;
            //Element.OnMouseLeave += ImageElement_OnMouseLeave;
            //Element.OnMouseMove += ImageElement_OnMouseMove;
        }
        void DetachImageElementEvents()
        {
            Element.OnLoad -= ImageElement_OnLoad;
            ////Element.OnClick -= ImageElement_OnClick;
            //Element.OnMouseEnter -= ImageElement_OnMouseEnter;
            //Element.OnMouseLeave -= ImageElement_OnMouseLeave;
            //Element.OnMouseMove -= ImageElement_OnMouseMove;
        }
        public bool IsDisposed { get; private set; } = false;
        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            if (Element != null)
            {
                // detach events 
                DetachImageElementEvents();
                Element.Dispose();
            }
            if (OverlayStyle != null)
            {
                OverlayStyle.Dispose();
                OverlayStyle = null;
            }
        }
        HTMLElement? OverlayContainerElement { get; set; }
        HTMLElement? OverlayElement { get; set; }
        HTMLCanvasElement? OverlayCanvasElement { get; set; }
        void ImageElement_OnMouseEnter(Event e)
        {
            JS.Log("ImageElement_OnMouseEnter", UID);
            //ShowOverlay();
            if (MeetsMinSizeRequirements == true)
            {
                //CreateOverlay();
                // queue or requeue this image. 
                //// when the user moves their mouse over an image it should get moved to the front of the queue for better user experience
                //_ = UpdateAnaglyph();
            }
        }
        //void ImageElement_OnMouseMove(Event e)
        //{
        //    //JS.Log("ImageElement_OnMouseMove", UID);

        //}
        bool overlayVisible = false;
        void ShowOverlay()
        {
            if (OverlayStyle != null)
            {
                // make visible
                OverlayStyle.SetProperty("display", "block");
                overlayVisible = true;
            }
        }
        void HideOverlay()
        {
            overlayVisible = false;
            if (OverlayStyle == null) return;
            OverlayStyle.SetProperty("display", "none");
        }
        public bool HasOverrideTag(string location)
        {
            return UsedOverrides.Contains(location);
        }
        public string AddOverrideTag(string location)
        {
            if (!UsedOverrides.Contains(location))
            {
                UsedOverrides.Add(location);
            }
            return location;
        }
        public event Action<TrackedImage> OnImageLoaded = default!;
        void ImageElement_OnLoad(Event e)
        {
            //JS.Log("ImageElement_OnLoad", UID);
            var currentSrc = ImageElement.CurrentSrc;
            if (!HasOverrideTag(currentSrc))
            {
                if (OriginalSource != currentSrc)
                {
                    OriginalSource = currentSrc;
                }
            }
            OverlayStyle?.SetProperty("background-color", "green");
            OnImageLoaded?.Invoke(this);
        }
    }
}
