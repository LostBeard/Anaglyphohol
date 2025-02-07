using SpawnDev.BlazorJS.BrowserExtension;
using SpawnDev.BlazorJS.BrowserExtension.Services;

namespace Anaglyphohol.Services
{
    public class SyncStorageService
    {
        StorageArea? SyncStorage { get; set; }
        BrowserExtensionService BrowserExtensionService;
        public SyncStorageService(BrowserExtensionService browserExtensionService)
        {
            BrowserExtensionService = browserExtensionService;
            SyncStorage = BrowserExtensionService.Browser?.Storage?.Sync;
        }
    }
}
