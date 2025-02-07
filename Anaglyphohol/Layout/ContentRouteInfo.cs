using SpawnDev.BlazorJS.BrowserExtension;
using System.Reflection;

namespace Anaglyphohol.Layout
{
    public class ContentRouteInfo
    {
        public Assembly Assembly { get; init; }
        public Type ComponentType { get; init; }
        public List<ContentLocationAttribute> ContentLocations { get; init; }
    }
}
