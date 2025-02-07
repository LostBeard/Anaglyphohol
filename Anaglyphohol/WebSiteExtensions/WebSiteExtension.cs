using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.BrowserExtension.Services;
using SpawnDev.BlazorJS.JSObjects;
using Window = SpawnDev.BlazorJS.JSObjects.Window;
using Action = System.Action;
using Timer = System.Timers.Timer;

namespace Anaglyphohol.WebSiteExtensions
{
    public class WebSiteExtension : IDisposable
    {
        public Uri LocationUrl { get; set; }
        public List<WatchNode> WatchNodes { get; set; } = new List<WatchNode>();
        public Document? Document { get; private set; }
        public Window? Window { get; private set; }
        public MutationObserver? BodyObserver { get; private set; }
        public BlazorJSRuntime JS;
        public BrowserExtensionService BrowserExtensionService { get; private set; }
        Timer currentTimeUpdateTimer = new Timer();
        public WebSiteExtension(BlazorJSRuntime js, BrowserExtensionService browserExtensionService)
        {
            JS = js;
            BrowserExtensionService = browserExtensionService;
            if (JS.GlobalScope == GlobalScope.Window)
            {
                // Window
                Window = JS.Get<Window>("window");
                Document = JS.Get<Document>("document");
            }
            // watch for page content changes
            BodyObserver = new MutationObserver(Callback.Create<Array<MutationRecord>, MutationObserver>(BodyObserver_Observed));
            // watch for url changes
            BrowserExtensionService.OnLocationChanged += BrowserExtensionService_OnLocationChanged;
            BrowserExtensionService_OnLocationChanged(BrowserExtensionService.LocationUri);
            currentTimeUpdateTimer.Interval = 1000;
            currentTimeUpdateTimer.Elapsed += CurrentTimeUpdateTimer_Elapsed;
            currentTimeUpdateTimer.Enabled = true;
        }
        private void CurrentTimeUpdateTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (LocationSupported)
                {
                    WatchedNodesUpdate();
                }
            }
            catch (Exception ex)
            {
                JS.Log($"CurrentTimeUpdateTimer_Elapsed error: {ex.Message} {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Returns true if all watch nodes given are found
        /// </summary>
        /// <param name="watchNodeNames"></param>
        /// <returns></returns>
        public bool Found(params string[] watchNodeNames)
        {
            var watchNodes = WatchNodes.Where(o => watchNodeNames.Contains(o.Name)).ToList();
            if (watchNodes.Count != watchNodeNames.Length)
            {
                //throw new Exception("Watch node name not found in watchNodeNames");
                return false;
            }
            var allFound = watchNodes.Where(o => o.Found).Count() == watchNodes.Count;
            return allFound;
        }
        /// <summary>
        /// Returns true if all watch nodes given are not found
        /// </summary>
        /// <param name="watchNodeNames"></param>
        /// <returns></returns>
        public bool NotFound(params string[] watchNodeNames)
        {
            var watchNodes = WatchNodes.Where(o => watchNodeNames.Contains(o.Name)).ToList();
            if (watchNodes.Count != watchNodeNames.Length)
            {
                //throw new Exception("Watch node name not found in watchNodeNames");
            }
            var allNotFound = watchNodes.Where(o => !o.Found).Count() == watchNodes.Count;
            return allNotFound;
        }
        public delegate void WatchedNodesUpdatedDelegate(List<string> found, List<string> lost);
        public event Action<List<string>> OnWatchedNodesUpdated;
        public virtual void WatchedNodesUpdate()
        {
            var changed = new List<string>();
            foreach (var watchNode in WatchNodes)
            {
                using var el = watchNode.Query(Document);// Document?.QuerySelector(watchNode.Selector);
                if (el != null && !watchNode.Found)
                {
                    watchNode.Found = true;
#if DEBUG
                    JS.Log($"WatchNode Found: {watchNode.Name}");
#endif
                    watchNode.OnFound?.Invoke(watchNode);
                    OnWatchNodeFound?.Invoke(watchNode);
                    changed.Add(watchNode.Name);
                }
                else if (el == null && watchNode.Found)
                {
                    watchNode.Found = false;
                    OnWatchNodeLost?.Invoke(watchNode);
                    watchNode.OnLost?.Invoke(watchNode);
#if DEBUG
                    JS.Log($"WatchNode Lost: {watchNode.Name}");
#endif
                    changed.Add(watchNode.Name);
                }
            }
            OnWatchedNodesUpdated?.Invoke(changed);
        }
        public bool LocationSupported { get; protected set; } = false;
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
            if (locationSupported)
            {
                WatchedNodesUpdate();
            }
        }

        public delegate void BodyObserverObservedDelegate(Array<MutationRecord> mutations, MutationObserver sender);
        public event BodyObserverObservedDelegate OnBodyObserverObserved;

        void BodyObserver_Observed(Array<MutationRecord> mutations, MutationObserver sender)
        {
            JS.Log("BodyObserver_Observed");
            OnBodyObserverObserved?.Invoke(mutations, sender);
            if (LocationSupported)
            {
                WatchedNodesUpdate();
            }
        }

        public WatchNode? GetWatchNode(string name) => WatchNodes.FirstOrDefault(o => o.Name == name);
        public Element? GetWatchNodeEl(string name) => WatchNodes.FirstOrDefault(o => o.Name == name)?.Query(Document);
        public TElement? GetWatchNodeEl<TElement>(string name) where TElement : Element => WatchNodes.FirstOrDefault(o => o.Name == name)?.Query<TElement>(Document);

        public void Dispose()
        {
            if (currentTimeUpdateTimer != null)
            {
                currentTimeUpdateTimer.Stop();
                currentTimeUpdateTimer.Dispose();
            }
            if (BodyObserver != null)
            {
                BodyObserver.Disconnect();
                BodyObserver.Dispose();
                BodyObserver = null;
            }
            Window?.Dispose();
            Window = null;
            Document?.Dispose();
            Document = null;
        }
    }
}
