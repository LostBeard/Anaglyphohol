using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.BrowserExtension.Services;

namespace Anaglyphohol.Services
{
    public class AdLogger
    {
        BlazorJSRuntime JS;
        public string InstanceId = Guid.NewGuid().ToString();
        BrowserExtensionService BrowserExtensionService;
        public AdLogger(BlazorJSRuntime js, BrowserExtensionService browserExtensionService)
        {
            JS = js;
            BrowserExtensionService = browserExtensionService;
        }

        public enum AdLogMarker
        {
            None = 0,
            AdStart,
            AdEnd,
        }

        public void Log(AdLogMarker marker)
        {

        }

        public void AdStart(string message)
        {

        }
    }
}
