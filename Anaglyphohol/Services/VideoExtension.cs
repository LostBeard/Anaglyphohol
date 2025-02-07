using Microsoft.AspNetCore.Components;
using SpawnDev.BlazorJS.BrowserExtension.Services;
using SpawnDev.BlazorJS.BrowserExtension;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS;
using Anaglyphohol.ExtensionContent;
using Anaglyphohol.WebSiteExtensions;
using Window = SpawnDev.BlazorJS.JSObjects.Window;
using Action = System.Action;
using Timer = System.Timers.Timer;

namespace Anaglyphohol.Services
{
    /// <summary>
    /// Extension content script for tracking video elements on a website
    /// </summary>
    public class VideoExtension : IDisposable
    {
        public Document? Document { get; private set; }
        public Window? Window { get; private set; }
        public MutationObserver? BodyObserver { get; private set; }
        public BlazorJSRuntime JS;
        public BrowserExtensionService BrowserExtensionService { get; private set; }
        ContentBridgeService ContentBridge;
        Timer currentTimeUpdateTimer = new Timer();
        public VideoExtension(BlazorJSRuntime js, BrowserExtensionService browserExtensionService, ContentBridgeService contentBridgeService)
        {
            JS = js;
            BrowserExtensionService = browserExtensionService;
            ContentBridge = contentBridgeService;
            if (JS.GlobalScope == GlobalScope.Window)
            {
                // Window
                Window = JS.Get<Window>("window");
                Document = JS.Get<Document>("document");
                currentTimeUpdateTimer.Interval = 1000;
                currentTimeUpdateTimer.Elapsed += CurrentTimeUpdateTimer_Elapsed;
                currentTimeUpdateTimer.Enabled = true;
                // watch for page content changes
                BodyObserver = new MutationObserver(Callback.Create<Array<MutationRecord>, MutationObserver>(BodyObserver_Observed));
                // watch for url changes
                BrowserExtensionService.OnLocationChanged += BrowserExtensionService_OnLocationChanged;
                // call BrowserExtensionService_OnLocationChanged now to handle the current url
                BrowserExtensionService_OnLocationChanged(BrowserExtensionService.LocationUri);
                // call ElementUpdate now to handle the current page content
                ElementUpdate();
            }
        }
        private void CurrentTimeUpdateTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {

        }
        public Dictionary<string, WebsiteVideo> Videos { get; } = new Dictionary<string, WebsiteVideo>();
        public delegate void WatchedNodesUpdatedDelegate(List<string> found, List<string> lost);
        public event Action<List<string>> OnWatchedNodesUpdated;
        void ElementUpdate()
        {
            var changed = false;
            var videoElements = Document!.QuerySelectorAll<HTMLVideoElement>("video").Using(nodeList => nodeList.ToList());
            var videoIdsFound = new List<string>();
            foreach (Node node in videoElements)
            {
                var video = node.JSRefMove<HTMLVideoElement>();
                var videoId = WebsiteVideo.GetVideoElementVideoId(video);
                if (string.IsNullOrEmpty(videoId))
                {
                    var websiteVideo = new WebsiteVideo(video, ContentBridge, JS);
                    videoIdsFound.Add(websiteVideo.VideoId);
                    Videos.Add(websiteVideo.VideoId, websiteVideo);
                    VideoElementFound(websiteVideo);
                    changed = true;
                }
                else
                {
                    videoIdsFound.Add(videoId);
                    video.Dispose();
                }
            }
            var lostIds = Videos.Keys.Except(videoIdsFound);
            foreach (var lostId in lostIds)
            {
                if (Videos.TryGetValue(lostId, out var websiteVideo))
                {
                    changed = true;
                    Videos.Remove(lostId);
                    VideoElementLost(websiteVideo);
                    websiteVideo.Dispose();
                }
            }
            if (changed)
            {
                Console.WriteLine("OnVideoCountChanged");
                OnVideoCountChanged?.Invoke();
            }
        }
        void Video_OnPlaying(Event ev)
        {
            using var videoEl = ev.TargetAs<HTMLVideoElement>();
            var videoId = WebsiteVideo.GetVideoElementVideoId(videoEl);
            if (!string.IsNullOrEmpty(videoId) && Videos.TryGetValue(videoId, out var websiteVideo))
            {
                var isPlaying = websiteVideo.VideoElement.IsPlaying();
                var ended = websiteVideo.VideoElement.Ended;
                var readyState = websiteVideo.VideoElement.ReadyState;
                JS.Log($"Video_OnPlaying: {isPlaying} {ended} {readyState} {websiteVideo.VideoId} {websiteVideo.VideoElement.Src}");
                OnVideoStateChanged?.Invoke();
            }
        }
        void Video_OnPlay(Event ev)
        {
            using var videoEl = ev.TargetAs<HTMLVideoElement>();
            var videoId = WebsiteVideo.GetVideoElementVideoId(videoEl);
            if (!string.IsNullOrEmpty(videoId) && Videos.TryGetValue(videoId, out var websiteVideo))
            {
                var isPlaying = websiteVideo.VideoElement.IsPlaying();
                var ended = websiteVideo.VideoElement.Ended;
                var readyState = websiteVideo.VideoElement.ReadyState;
                JS.Log($"Video_OnPlay: {isPlaying} {ended} {readyState} {websiteVideo.VideoId} {websiteVideo.VideoElement.Src}");
                OnVideoStateChanged?.Invoke();
            }
        }
        void Video_OnPause(Event ev)
        {
            using var videoEl = ev.TargetAs<HTMLVideoElement>();
            var videoId = WebsiteVideo.GetVideoElementVideoId(videoEl);
            if (!string.IsNullOrEmpty(videoId) && Videos.TryGetValue(videoId, out var websiteVideo))
            {
                var paused = websiteVideo.VideoElement.Paused;
                var ended = websiteVideo.VideoElement.Ended;
                var readyState = websiteVideo.VideoElement.ReadyState;
                var isPlaying = websiteVideo.VideoElement.IsPlaying();
                JS.Log($"Video_OnPause: {isPlaying} {ended} {readyState} {websiteVideo.VideoId} {websiteVideo.VideoElement.Src}");
                OnVideoStateChanged?.Invoke();
            }
        }
        void Video_OnEmptied(Event ev)
        {
            using var videoEl = ev.TargetAs<HTMLVideoElement>();
            var videoId = WebsiteVideo.GetVideoElementVideoId(videoEl);
            if (!string.IsNullOrEmpty(videoId) && Videos.TryGetValue(videoId, out var websiteVideo))
            {
                var paused = websiteVideo.VideoElement.Paused;
                var ended = websiteVideo.VideoElement.Ended;
                var readyState = websiteVideo.VideoElement.ReadyState;
                var isPlaying = websiteVideo.VideoElement.IsPlaying();
                JS.Log($"Video_OnEmptied: {isPlaying} {ended} {readyState} {websiteVideo.VideoId} {websiteVideo.VideoElement.Src}");
                OnVideoStateChanged?.Invoke();
            }
        }
        void Video_OnEnded(Event ev)
        {
            using var videoEl = ev.TargetAs<HTMLVideoElement>();
            var videoId = WebsiteVideo.GetVideoElementVideoId(videoEl);
            if (!string.IsNullOrEmpty(videoId) && Videos.TryGetValue(videoId, out var websiteVideo))
            {
                var paused = websiteVideo.VideoElement.Paused;
                var ended = websiteVideo.VideoElement.Ended;
                var readyState = websiteVideo.VideoElement.ReadyState;
                var isPlaying = websiteVideo.VideoElement.IsPlaying();
                JS.Log($"Video_OnEnded: {isPlaying} {ended} {readyState} {websiteVideo.VideoId} {websiteVideo.VideoElement.Src}");
                OnVideoStateChanged?.Invoke();
            }
        }
        void Video_OnDurationChange(Event ev)
        {
            using var videoEl = ev.TargetAs<HTMLVideoElement>();
            var videoId = WebsiteVideo.GetVideoElementVideoId(videoEl);
            if (!string.IsNullOrEmpty(videoId) && Videos.TryGetValue(videoId, out var websiteVideo))
            {
                var paused = websiteVideo.VideoElement.Paused;
                var ended = websiteVideo.VideoElement.Ended;
                var readyState = websiteVideo.VideoElement.ReadyState;
                var isPlaying = websiteVideo.VideoElement.IsPlaying();
                JS.Log($"Video_OnDurationChange: {isPlaying} {ended} {readyState} {websiteVideo.VideoId} {websiteVideo.VideoElement.Src}");
                OnVideoStateChanged?.Invoke();
            }
        }
        void Video_OnTimeUpdate(Event ev)
        {
            using var videoEl = ev.TargetAs<HTMLVideoElement>();
            var videoId = WebsiteVideo.GetVideoElementVideoId(videoEl);
            if (!string.IsNullOrEmpty(videoId) && Videos.TryGetValue(videoId, out var websiteVideo))
            {
                var paused = websiteVideo.VideoElement.Paused;
                var ended = websiteVideo.VideoElement.Ended;
                var readyState = websiteVideo.VideoElement.ReadyState;
                var isPlaying = websiteVideo.VideoElement.IsPlaying();
                //JS.Log($"Video_OnTimeUpdate: {isPlaying} {ended} {readyState} {websiteVideo.VideoId} {websiteVideo.VideoElement.Src}");
                OnVideoStateChanged?.Invoke();
            }
        }
        public event Action OnVideoCountChanged;
        public event Action OnVideoStateChanged;
        void VideoElementFound(WebsiteVideo videoElement)
        {
            JS.Log($"VideoElementFound: {videoElement.VideoId}");
            //var ext = videoElement.Player != null ? "++" : "";
            //JS.Log($"ParamountPlusVideo{ext} element found", videoElement.VideoId, Videos.Count);
            videoElement.VideoElement.OnPlay += Video_OnPlay;
            videoElement.VideoElement.OnPause += Video_OnPause;
            videoElement.VideoElement.OnPlaying += Video_OnPlaying;
            videoElement.VideoElement.OnDurationChange += Video_OnDurationChange;
            videoElement.VideoElement.OnEmptied += Video_OnEmptied;
            videoElement.VideoElement.OnEnded += Video_OnEnded;
            videoElement.VideoElement.OnTimeUpdate += Video_OnTimeUpdate;
            //UpdateOverflowCheck();
        }
        void VideoElementLost(WebsiteVideo videoElement)
        {
            JS.Log($"VideoElementLost: {videoElement.VideoId}");

            //var ext = videoElement.Player != null ? "++" : "";
            //JS.Log($"ParamountPlusVideo{ext} element lost", videoElement.VideoId, Videos.Count);
            videoElement.VideoElement.OnPlay -= Video_OnPlay;
            videoElement.VideoElement.OnPause -= Video_OnPause;
            videoElement.VideoElement.OnPlaying -= Video_OnPlaying;
            videoElement.VideoElement.OnDurationChange -= Video_OnDurationChange;
            videoElement.VideoElement.OnEmptied -= Video_OnEmptied;
            videoElement.VideoElement.OnEnded -= Video_OnEnded;
            videoElement.VideoElement.OnTimeUpdate -= Video_OnTimeUpdate;
            //UpdateOverflowCheck();
        }
        public bool LocationSupported { get; protected set; }
        protected virtual bool LocationSupportedCheck(Uri locationUri)
        {
            return true;
        }
        public bool FullscreenWindowCheck(bool requireFullWidth = true, bool requireFullHeight = true)
        {
            if (Window == null) return false;
            using var screen = Window.Screen;
            if (screen == null) return false;
            return (!requireFullHeight || screen.Height == Window.InnerHeight) && (!requireFullWidth || screen.Width == Window.InnerWidth);
        }

        public event Action<bool> OnLocationSupportedChanged;
        public event Action<WatchNode> OnWatchNodeFound;
        public event Action<WatchNode> OnWatchNodeLost;
        private void BrowserExtensionService_OnLocationChanged(Uri locationUri)
        {
            // the location has changed
            var locationSupported = LocationSupportedCheck(locationUri);
            if (LocationSupported != locationSupported)
            {
                LocationSupported = locationSupported;
                OnLocationSupportedChanged?.Invoke(LocationSupported);
                if (LocationSupported)
                {
                    using var body = Document?.QuerySelector<HTMLBodyElement>("body");
                    if (body != null)
                    {
                        BodyObserver?.Observe(body, new MutationObserveOptions { ChildList = true, Subtree = true });
                    }
                }
                else
                {
                    BodyObserver?.Disconnect();
                }
            }
        }

        public delegate void BodyObserverObservedDelegate(Array<MutationRecord> mutations, MutationObserver sender);
        public event BodyObserverObservedDelegate OnBodyObserverObserved;

        void BodyObserver_Observed(Array<MutationRecord> mutations, MutationObserver sender)
        {
            OnBodyObserverObserved?.Invoke(mutations, sender);
            ElementUpdate();
        }

        public void Dispose()
        {
            if (BodyObserver != null)
            {
                BodyObserver.Disconnect();
                BodyObserver.Dispose();
                BodyObserver = null;
            }
        }
    }
}
