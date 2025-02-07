using SpawnDev.BlazorJS.JSObjects;

namespace Anaglyphohol.Services
{
    public static class HTMLVideoElementExtensions
    {
        public static bool IsPlaying(this HTMLVideoElement _this)
        {
            return !_this.Paused && !_this.Ended && _this.ReadyState >= 2;
        }
    }
}
