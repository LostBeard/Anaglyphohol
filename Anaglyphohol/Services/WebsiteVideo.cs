using SpawnDev.BlazorJS.BrowserExtension.Services;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS;

namespace Anaglyphohol.Services
{
    public class WebsiteVideo : IDisposable
    {
        public const string VideoIdTag = "__extensionVideoId";
        public static string? GetVideoElementVideoId(HTMLVideoElement videoElement) => videoElement.JSRef!.Get<string?>(VideoIdTag);
        public static void SetVideoElementVideoId(HTMLVideoElement videoElement, string videoId) => videoElement.JSRef!.Set(VideoIdTag, videoId);
        public string VideoId { get; private set; }
        BlazorJSRuntime JS;
        ContentBridgeService ContentBridge;
        public HTMLVideoElement VideoElement { get; private set; }
        public double CurrentTime { get; set; }
        public WebsiteVideo(string videoId, HTMLVideoElement videoElement, ContentBridgeService contentBridge, BlazorJSRuntime js)
        {
            VideoId = videoId;
            JS = js;
            ContentBridge = contentBridge;
            VideoElement = videoElement;
            AttachVideoElementEvents();
            UpdateVideoElement();
        }
        public WebsiteVideo(HTMLVideoElement videoElement, ContentBridgeService contentBridge, BlazorJSRuntime js)
        {
            VideoId = GetVideoElementVideoId(videoElement) ?? "";
            if (string.IsNullOrEmpty(VideoId))
            {
                VideoId = Guid.NewGuid().ToString();
                SetVideoElementVideoId(videoElement, VideoId);
            }
            JS = js;
            ContentBridge = contentBridge;
            VideoElement = videoElement;
            AttachVideoElementEvents();
            UpdateVideoElement();
        }
        void AttachVideoElementEvents()
        {
            VideoElement.OnLoadedMetadata += VideoElement_OnLoadedMetadata;
            VideoElement.OnPlay += VideoElement_OnPlay;
            VideoElement.OnPlaying += VideoElement_OnPlaying;
            VideoElement.OnPause += VideoElement_OnPause;
            VideoElement.OnVolumeChange += VideoElement_OnVolumeChange;
            VideoElement.OnDurationChange += VideoElement_OnDurationChange;
            VideoElement.OnTimeUpdate += VideoElement_OnTimeUpdate;
            VideoElement.OnEmptied += VideoElement_OnEmptied;
            VideoElement.OnEnded += VideoElement_OnEnded;
        }
        void DetachVideoElementEvents()
        {
            VideoElement.OnLoadedMetadata -= VideoElement_OnLoadedMetadata;
            VideoElement.OnPlay -= VideoElement_OnPlay;
            VideoElement.OnPlaying -= VideoElement_OnPlaying;
            VideoElement.OnPause -= VideoElement_OnPause;
            VideoElement.OnVolumeChange -= VideoElement_OnVolumeChange;
            VideoElement.OnDurationChange -= VideoElement_OnDurationChange;
            VideoElement.OnTimeUpdate -= VideoElement_OnTimeUpdate;
            VideoElement.OnEmptied -= VideoElement_OnEmptied;
            VideoElement.OnEnded -= VideoElement_OnEnded;
        }
        public bool IsDisposed { get; private set; } = false;
        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            if (VideoElement != null)
            {
                // detach events 
                DetachVideoElementEvents();
                VideoElement.Dispose();
            }
        }
        void UpdateVideoElement()
        {

        }
        void VideoElement_OnEnded(Event e)
        {
            JS.Log("VideoElement_OnEnded", VideoId);
            // get the video element proxy for website side of given element
            UpdateVideoElement();
        }
        void VideoElement_OnEmptied(Event e)
        {
            JS.Log("VideoElement_OnEmptied", VideoId);
            // get the video element proxy for website side of given element
            UpdateVideoElement();
        }
        void VideoElement_OnLoadedMetadata(Event e)
        {
            JS.Log("VideoElement_OnLoadedMetadata", VideoId);
            // get the video element proxy for website side of given element
            UpdateVideoElement();
        }
        void VideoElement_OnTimeUpdate(Event e)
        {
            
        }
        void VideoElement_OnDurationChange(Event e)
        {
            JS.Log("VideoElement_OnDurationChange", VideoId, VideoElement.CurrentTime, VideoElement.Duration);
        }
        void VideoElement_OnVolumeChange(Event e)
        {
            JS.Log("VideoElement_OnVolumeChange", VideoId, VideoElement.Volume, VideoElement.Muted);
        }
        void VideoElement_OnPlay(Event e)
        {
            JS.Log("VideoElement_OnPlay", VideoId);

        }
        void VideoElement_OnPlaying(Event e)
        {
            JS.Log("VideoElement_OnPlaying", VideoId);
            UpdateVideoElement();
        }
        void VideoElement_OnPause(Event e)
        {
            JS.Log("VideoElement_OnPause", VideoId);

        }
    }
}
