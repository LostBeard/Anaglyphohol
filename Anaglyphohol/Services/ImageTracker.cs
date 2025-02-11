using Anaglyphohol.MultiviewMaker;
using Anaglyphohol.WebSiteExtensions;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.BrowserExtension.Services;
using SpawnDev.BlazorJS.JSObjects;
using System.Reflection.Metadata.Ecma335;
using Action = System.Action;
using Window = SpawnDev.BlazorJS.JSObjects.Window;

namespace Anaglyphohol.Services
{
    /// <summary>
    /// Extension content script for tracking elements on a website
    /// </summary>
    public class ImageTracker : IDisposable
    {
        public Document? Document { get; private set; }
        public Window? Window { get; private set; }
        public MutationObserver? BodyObserver { get; private set; }
        public BlazorJSRuntime JS;
        public BrowserExtensionService BrowserExtensionService { get; private set; }
        ContentBridgeService ContentBridge;
        public Dictionary<string, TrackedImage> TrackedElements { get; } = new Dictionary<string, TrackedImage>();
        public delegate void WatchedNodesUpdatedDelegate(List<string> found, List<string> lost);
        public delegate void BodyObserverObservedDelegate(Array<MutationRecord> mutations, MutationObserver sender);
        public event BodyObserverObservedDelegate OnBodyObserverObserved;
        public event Action OnTrackedElementCountChanged;
        public event Action OnStateChanged;
        AnaglyphImageMakerService AnaglyphImageMakerService;

        bool _AutoAnaglyphAll = false;
        public bool AutoAnaglyphAll
        {
            get => _AutoAnaglyphAll;
            set
            {
                _AutoAnaglyphAll = value;
                _ = CheckTrackedElementsDelayed();
            }
        }
        public int AnaglyphProfile
        {
            get => AnaglyphImageMakerService.AnaglyphProfile;
            set
            {
                if (AnaglyphImageMakerService.AnaglyphProfile == value) return;
                AnaglyphImageMakerService.AnaglyphProfile = value;
                _ = CheckTrackedElementsDelayed();
            }
        }

        async Task CheckTrackedElementsDelayed()
        {
            await Task.Delay(100);
            CheckTrackedElements();
        }
        void CheckTrackedElements()
        {
            foreach (var trackedElement in TrackedElements.Values)
            {
                CheckTrackedElement(trackedElement);
            }
        }

        public ImageTracker(BlazorJSRuntime js, BrowserExtensionService browserExtensionService, ContentBridgeService contentBridgeService, AnaglyphImageMakerService anaglyphImageMakerService)
        {
            JS = js;
            AnaglyphImageMakerService = anaglyphImageMakerService;
            BrowserExtensionService = browserExtensionService;
            ContentBridge = contentBridgeService;
            if (JS.GlobalScope == GlobalScope.Window)
            {
                // Window
                Window = JS.Get<Window>("window");
                Document = JS.Get<Document>("document");
            }
        }
        public bool Started { get; private set; }
        ActionCallback<Array<MutationRecord>, MutationObserver>? BodyObserverObservedCallback = null;
        public void Start()
        {
            if (Started) return;
            Started = true;
            using var body = Document?.QuerySelector<HTMLBodyElement>("body");
            if (body != null)
            {
                BodyObserver = new MutationObserver(BodyObserverObservedCallback = new ActionCallback<Array<MutationRecord>, MutationObserver>(BodyObserver_Observed));
                BodyObserver.Observe(body, new MutationObserveOptions { ChildList = true, Subtree = true });
            }
            ElementUpdate();
        }
        public void StopIt()
        {
            if (!Started) return;
            Started = false;
            if (BodyObserver != null)
            {
                BodyObserverObservedCallback?.Dispose();
                BodyObserverObservedCallback = null;
                BodyObserver.Disconnect();
                BodyObserver.Dispose();
                BodyObserver = null;
            }
        }
        void ElementUpdate()
        {
            var changed = false;
            List<HTMLElement> elements = Document!.QuerySelectorAll<HTMLImageElement>("img").Using(nodeList => nodeList.ToList()).ToList<HTMLElement>();
            //foreach(var BackgroundImageDivSelector in BackgroundImageDivs)
            //{
            //    var backgroundDivs = Document!.QuerySelectorAll<HTMLDivElement>(BackgroundImageDivSelector).Using(nodeList => nodeList.ToList());
            //    foreach (var backgroundDiv in backgroundDivs)
            //    {
            //        var backgroundImage = backgroundDiv.Style.GetPropertyValue("background-image");
            //        if (!string.IsNullOrEmpty(backgroundImage) && backgroundImage.StartsWith("url("))
            //        {
            //            elements.Add(backgroundDiv);
            //        }
            //    }
            //}
            var uidsFound = new List<string>();
            foreach (var el in elements)
            {
                // ignore elements marked as do not track
                var doNotTrack = TrackedImage.GetElementDoNotTrack(el);
                if (doNotTrack == true) continue;
                var uid = TrackedImage.GetElementUID(el);
                if (string.IsNullOrEmpty(uid))
                {
                    var trackedElement = new TrackedImage(el, JS);
                    uidsFound.Add(trackedElement.UID);
                    TrackedElements.Add(trackedElement.UID, trackedElement);
                    trackedElement.OnImageLoaded += TrackedElement_OnImageLoaded;
                    TrackedElementFound(trackedElement);
                    changed = true;
                }
                else
                {
                    uidsFound.Add(uid);
                    el.Dispose();
                }
            }
            var uidsLost = TrackedElements.Keys.Except(uidsFound);
            foreach (var lostId in uidsLost)
            {
                if (TrackedElements.TryGetValue(lostId, out var trackedElement))
                {
                    changed = true;
                    TrackedElements.Remove(lostId);
                    TrackedElementLost(trackedElement);
                    trackedElement.OnImageLoaded -= TrackedElement_OnImageLoaded;
                    trackedElement.Dispose();
                }
            }
            if (changed)
            {
                //Console.WriteLine("OnTrackedElementCountChanged");
                OnTrackedElementCountChanged?.Invoke();
                OnStateChanged?.Invoke();
            }
        }
        private void TrackedElement_OnImageLoaded(TrackedImage trackedElement)
        {
            CheckTrackedElement(trackedElement);
        }
        bool Running = false;
        List<TrackedImage> ToAnaglyph = new List<TrackedImage>();
        /// <summary>
        /// Checks the tracked element to see if it should be added to the queue for processing
        /// </summary>
        /// <param name="trackedElement"></param>
        /// <param name="jumpToFrontOfLine"></param>
        void CheckTrackedElement(TrackedImage trackedElement, bool jumpToFrontOfLine = false)
        {
            if (CurrentJob?.UID == trackedElement.UID)
            {
                // this element is currently being processed
                return;
            }
            if (AutoAnaglyphAll)
            {
                trackedElement.SetState(!string.IsNullOrEmpty(trackedElement.OverrideSrc) ? "anaglyph" : "failed");
                if (trackedElement.MeetsMinSizeRequirements != true)
                {
                    trackedElement.SetState("");
                    return;
                }
                trackedElement.UseOverride = true;
                var anaglyphSettingsMatch = trackedElement.Level3D == AnaglyphImageMakerService.Level3D &&
                trackedElement.Focus3D == AnaglyphImageMakerService.Focus3D &&
                trackedElement.AnaglyphProfileId == AnaglyphImageMakerService.AnaglyphProfile;
                if (!trackedElement.IsOverrideSrc || !anaglyphSettingsMatch)
                {
                    //Console.WriteLine($"Enqueueing: {trackedElement.UID}");
                    var index = ToAnaglyph.IndexOf(trackedElement);
                    if (index != -1)
                    {
                        if (jumpToFrontOfLine && index != 0)
                        {
                            //Console.WriteLine($"Jumping to front of the line: {trackedElement.UID}");
                            ToAnaglyph.Remove(trackedElement);
                            // insert at front of line
                            ToAnaglyph.Insert(0, trackedElement);
                        }
                        else
                        {
                            // nothing to do.
                        }
                    }
                    else if (jumpToFrontOfLine)
                    {
                        // insert at front of line
                        ToAnaglyph.Insert(0, trackedElement);
                        trackedElement.SetState("queued");
                        if (trackedElement.IsOverrideSrc)
                        {
                            // switch back to original before using 
                            _ = trackedElement.RestoreOriginal();
                            return;
                        }
                        OnStateChanged?.Invoke();
                    }
                    else
                    {
                        // insert at end of line
                        ToAnaglyph.Add(trackedElement);
                        trackedElement.SetState("queued");
                        if (trackedElement.IsOverrideSrc)
                        {
                            // switch back to original before using 
                            _ = trackedElement.RestoreOriginal();
                            return;
                        }
                        OnStateChanged?.Invoke();
                    }
                    if (ToAnaglyph.Any() && !Running)
                    {
                        Running = true;
                        _ = StartRun();
                    }
                }
            }
            else
            {
                trackedElement.UseOverride = false;
                if (ToAnaglyph.Contains(trackedElement))
                {
                    ToAnaglyph.Remove(trackedElement);
                }
                trackedElement.SetState("");
            }
            //Console.WriteLine($"~ ToAnaglyph.Count: {ToAnaglyph.Count}");
        }
        TrackedImage? CurrentJob = null;
        public bool IsBusy => TotalJobsQueued > 0;
        public float Progress
        {
            get
            {
                var total = CompatibleTrackedImages;
                if (total == 0) return 0;
                var done = total - TotalJobsQueued;
                return (float)done * 100f / (float)total;
            }
        }
        public int CompatibleTrackedImages => TrackedElements.Values.Count(o => o.MeetsMinSizeRequirements == true);
        public int TotalJobsQueued => ToAnaglyph.Count + (CurrentJob == null ? 0 : 1);
        async Task StartRun()
        {
            Running = true;
            //Console.WriteLine(">> StartRun");
            OnStateChanged?.Invoke();
            try
            {
                while (ToAnaglyph.Any())
                {
                    var trackedElement = ToAnaglyph[0];
                    ToAnaglyph.RemoveAt(0);
                    CurrentJob = trackedElement;
                    CurrentJob.SetState("active");
                    if (trackedElement.IsOverrideSrc)
                    {
                        // switch back to original before using 
                        await trackedElement.RestoreOriginal();
                    }
                    var anaglyphUrl = await AnaglyphImageMakerService.Update(trackedElement.ImageElement);
                    if (!string.IsNullOrEmpty(anaglyphUrl))
                    {
                        trackedElement.OverrideSrc = anaglyphUrl;
                        trackedElement.UseOverride = AutoAnaglyphAll;
                        trackedElement.Level3D = AnaglyphImageMakerService.Level3D;
                        trackedElement.Focus3D = AnaglyphImageMakerService.Focus3D;
                        trackedElement.AnaglyphProfileId = AnaglyphImageMakerService.AnaglyphProfile;
                        CurrentJob.SetState("anaglyph");
                    }
                    else
                    {
                        CurrentJob.SetState("failed");
                    }
                    //Console.WriteLine($"- ToAnaglyph.Count: {ToAnaglyph.Count}");
                    CurrentJob = null;
                    OnStateChanged?.Invoke();
                }
            }
            finally
            {
                //Console.WriteLine("<< StartRun");
                CurrentJob = null;
                Running = false;
                OnStateChanged?.Invoke();
            }
        }
        // active - 
        // failed
        // anaglyph
        // queued
        // ""
        void ImageElement_OnMouseEnter(MouseEvent ev)
        {
            using var target = ev.TargetAs<HTMLImageElement>();
            var uid = TrackedImage.GetElementUID(target);
            if (!string.IsNullOrEmpty(uid) && TrackedElements.TryGetValue(uid, out var trackedElement))
            {
                CheckTrackedElement(trackedElement, true);
            }
        }
        void TrackedElementFound(TrackedImage trackedElement)
        {
            //JS.Log($"TrackedElementFound: {trackedElement.UID}");
            trackedElement.Element.OnMouseEnter += ImageElement_OnMouseEnter;
            CheckTrackedElement(trackedElement);
        }
        void TrackedElementLost(TrackedImage trackedElement)
        {
            //JS.Log($"TrackedElementLost: {trackedElement.UID}");
            trackedElement.Element.OnMouseEnter -= ImageElement_OnMouseEnter;
        }
        public bool FullscreenWindowCheck(bool requireFullWidth = true, bool requireFullHeight = true)
        {
            if (Window == null) return false;
            using var screen = Window.Screen;
            if (screen == null) return false;
            return (!requireFullHeight || screen.Height == Window.InnerHeight) && (!requireFullWidth || screen.Width == Window.InnerWidth);
        }
        void BodyObserver_Observed(Array<MutationRecord> mutations, MutationObserver sender)
        {
            OnBodyObserverObserved?.Invoke(mutations, sender);
            ElementUpdate();
        }
        /// <inheritdoc />
        public void Dispose()
        {
            StopIt();
        }
    }
}
