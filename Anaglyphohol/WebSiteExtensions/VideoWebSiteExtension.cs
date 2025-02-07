using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.BrowserExtension.Services;
using SpawnDev.BlazorJS.JSObjects;
using Anaglyphohol.Services;

namespace Anaglyphohol.WebSiteExtensions
{
    public class VideoWebSiteExtension : WebSiteExtension
    {
        public VideoWebSiteExtension(BlazorJSRuntime js, BrowserExtensionService browserExtensionService) : base(js, browserExtensionService)
        {
            JS.Set("_setMuted", SetMuted);
        }

        /// <summary>
        /// Whether or not ads should be skipped automatically (if supported)
        /// </summary>
        public bool SkipAds { get; set; } = true;
        /// <summary>
        /// Whether videos should be put in full screen mode automatically (if supported)<br />
        /// Primarily for ParamountPlus and DisneyPlus which revert to non-fullscreen when the next episode of a show is automatically started
        /// </summary>
        public bool FullScreenVideos { get; set; } = false;
        /// <summary>
        /// Whether ads should be muted automatically (if supported)
        /// </summary>
        public bool MuteAds { get; set; } = true;
        /// <summary>
        /// Whether videos should be unmuted automatically, even if user muted (if supported)
        /// </summary>
        public bool UnmuteVideo { get; set; } = true;

        public void PlayFromStart()
        {
            using var el = GetWatchNodeEl<HTMLButtonElement>("restart");
            el?.Click();
        }
        public void ExitFullscreen()
        {
            using var el = GetWatchNodeEl<HTMLButtonElement>("exit-fullscreen");
            el?.Click();
        }
        public void Fullscreen()
        {
            using var el = GetWatchNodeEl<HTMLButtonElement>("fullscreen");
            el?.Click();
        }
        public virtual void SkipAd()
        {
            var wNode = WatchNodes.FirstOrDefault(o => o.Name.StartsWith("skipAd") && o.Found);
            if (wNode == null) return;
            using var el = GetWatchNodeEl<HTMLButtonElement>(wNode.Name);
            el?.Click();
        }
        public void ClickWatchedNodeButton(string name)
        {
            using var el = GetWatchNodeEl<HTMLButtonElement>(name);
            JS.Log("el1", el);
            JS.Set("el1", el);
            if (el == null)
            {
                Console.WriteLine($"el not found: {name}");
            }
            else
            {
                Console.WriteLine($"el found: {name}");
            }
            el?.Click();
        }
        public void Pause()
        {
            using var el = GetWatchNodeEl<HTMLButtonElement>("pause");
            el?.Click();
        }
        public bool Paused
        {
            get
            {
                using var video = GetWatchNodeEl<HTMLVideoElement>("video");
                return video != null && video.Paused;
            }
        }
        public void Play()
        {
            using var el = GetWatchNodeEl<HTMLButtonElement>("play");
            el?.Click();
        }
        public bool Playing
        {
            get
            {
                using var video = GetWatchNodeEl<HTMLVideoElement>("video");
                return video?.IsPlaying() ?? false;
            }
        }
        public double Volume
        {
            get
            {
                using var video = GetWatchNodeEl<HTMLVideoElement>("video");
                return video != null ? video.Volume : 1;
            }
            set
            {
#if DEBUG
                Console.WriteLine($"Muted being set: {value}");
#endif
                using var video = GetWatchNodeEl<HTMLVideoElement>("video");
                if (video != null)
                {
                    video.Volume = value;
                }
            }
        }
        public bool Muted
        {
            get
            {
                using var video = GetWatchNodeEl<HTMLVideoElement>("video");
                return video != null && video.Muted;
            }
            set
            {
#if DEBUG
                Console.WriteLine($"Muted being set: {value}");
#endif
                if (Muted == value) return;
                using var el = GetWatchNodeEl<HTMLButtonElement>("mute");
                el?.Click();
            }
        }
        public bool SetMuted(bool muted) => Muted = muted;
    }
}
