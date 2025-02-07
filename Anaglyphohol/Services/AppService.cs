using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.BrowserExtension;
using SpawnDev.BlazorJS.BrowserExtension.Services;
using SpawnDev.BlazorJS.JSObjects;
using Anaglyphohol.Background;

namespace Anaglyphohol.Services
{
    public class AppService : IAsyncBackgroundService
    {
        /// <summary>
        /// Returns when the service is Ready
        /// </summary>
        public Task Ready => _Ready ??= InitAsync();
        private Task? _Ready = null;
        BlazorJSRuntime JS;
        BrowserExtensionService BrowserExtensionService;
        BackgroundService BackgroundService;
        public AppService(BlazorJSRuntime js, BrowserExtensionService browserExtensionService, BackgroundService backgroundService)
        {
            JS = js;
            BrowserExtensionService = browserExtensionService;
            BackgroundService = backgroundService;
        }
        async Task InitAsync()
        {
            await BackgroundService.Ready;
            //JS.Log("AppService.InitAsync()");
            switch (BrowserExtensionService.ExtensionMode)
            {
                case ExtensionMode.Content:
                    InitContentMode();
                    break;
                case ExtensionMode.Background:
                    await InitBackgroundMode();
                    break;
                case ExtensionMode.ExtensionPage:
                    // WebWorkerService.Instances can be used to communicate with the BackgroundWorker instance
                    // In Chrome, calling runtime.connect() from the Options/Popup/Installed window instance never fired on the background worker.
                    // Worked in Firefox
                    break;
                case ExtensionMode.None:
                    // probably running in a website: window, worker, shared worker, or service worker
                    break;
                default:
                    break;
            }
        }
        //Port? BackgroundPort { get; set; }
        //List<Port> ContentPorts = new List<Port>();
        void InitContentMode()
        {
            var runtime = BrowserExtensionService.Runtime;
            //JS.Log("InitContentMode", runtime);
            if (runtime == null) return;
            runtime.OnMessage += BackgroundWorker_OnMessage;

#if DEBUG
            runtime.SendMessage("Hello from content");
#endif
            //BackgroundPort = runtime.Connect(new ConnectInfo { Name = "content-port" });
            //BackgroundPort.OnMessage += BackgroundPort_OnMessage;
            //BackgroundPort.OnDisconnect += BackgroundPort_OnDisconnect;
            //BackgroundPort.PostMessage("Hello background from content!");
        }
        /// <summary>
        /// When running in a background script, method receives messages to the background script<br/>
        /// When not running in a background script, method receives messages from the background script<br/>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="sender"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        bool BackgroundWorker_OnMessage(JSObject message, MessageSender sender, Function? callback)
        {
            var responseRequested = callback != null;
            var typeOfMessage = message.JSRef!.TypeOf();
            var cmd = typeOfMessage == "object" ? message.JSRef!.Get<string?>("cmd") : null;
            if (BrowserExtensionService.ExtensionMode == ExtensionMode.Background)
            {
                // message to background
#if DEBUG
                JS.Log("BackgroundWorker_OnMessage to bg", cmd, message, sender, callback);
                JS.Log("responseRequested:", responseRequested);
#endif
                switch (cmd)
                {
                    case "GetImage":
                        if (callback != null)
                        {
                            var url = message.JSRef!.Get<string?>("url");
                            _ = new Func<Task>(async () =>
                            {
                                try
                                {
                                    var resp = await GetImage(url);
                                    callback.CallVoid(null, resp);
                                }
                                catch(Exception ex)
                                {
                                    JS.Log("GetImage failed:", ex.Message);
                                    callback.CallVoid(null, "");
                                }
                            })();
                            return true;
                        }
                        break;
                    default:
                        JS.Log("Unknown cmd", cmd);
                        break;
                }
            }
            else
            {
#if DEBUG
                // message from background
                JS.Log("BackgroundWorker_OnMessage from bg", cmd, message, sender, callback);
                JS.Log("responseRequested:", responseRequested);
#endif
            }
            return false;
        }
        async Task InitBackgroundMode()
        {
            var runtime = BrowserExtensionService.Runtime;
#if DEBUG
            JS.Log("InitBackgroundMode1", runtime);
#endif
            if (runtime == null) return;
            runtime.OnMessage += BackgroundWorker_OnMessage;
            //runtime.OnConnect += Runtime_OnConnect;
            //using var tabs = BrowserExtensionService.Browser!.Tabs!;
            //var allTabs = await tabs.Query(new TabQueryInfo { });
            //JS.Log("allTabs1", allTabs);
            //JS.Set("allTabs1", allTabs);
            //var succ = 0;
            //var fail = 0;
            //foreach (var tab in allTabs)
            //{
            //    try
            //    {
            //        await tabs.SendMessage(tab.Id, "ping");
            //        succ++;
            //    }
            //    catch
            //    {
            //        fail++;
            //    }
            //}
            //JS.Log("ping:", fail, succ);
        }

        //      fetch('https://www.example.com/image.jpg')
        //.then(response => response.blob())
        //.then(blob => {
        //  const reader = new FileReader();
        //  reader.readAsDataURL(blob); 
        //  reader.onloadend = () => {
        //    const base64data = reader.result;
        //    console.log(base64data); 
        //    // Use the base64data URL here
        //  }
        //});
        
        public async Task<string> GetImage(string url)
        {
            //JS.Log($"GetImage called: {BrowserExtensionService.ExtensionMode}");
            switch (BrowserExtensionService.ExtensionMode)
            {
                case ExtensionMode.Background:
                    var response = await JS.Fetch(url);
                    if (response.Ok)
                    {
                        using var blob = await response.Blob();
                        var dataUrl = await FileReader.ReadAsDataURLAsync(blob);
                        //JS.Log("dataUrl", dataUrl);
                        return dataUrl;
                    }
                    throw new Exception("Failed");
                default:
                    var resp = await BrowserExtensionService.Runtime!.SendMessage<string>(new { cmd = "GetImage", url = url });
                    //JS.Log("bg resp ==", resp);
                    return resp;
            }
        }
        ///// <summary>
        ///// This event handler is fired when an extension context calls connect<br/>
        ///// We are listening for Blazor in extension content context
        ///// </summary>
        ///// <param name="port"></param>
        //void Runtime_OnConnect(Port port)
        //{
        //    JS.Log("Runtime_OnConnect", port);
        //    switch (port.Name)
        //    {
        //        case "content-port":
        //            ContentPorts.Add(port);
        //            port.OnMessage += ContentPort_OnMessage;
        //            port.OnDisconnect += ContentPort_OnDisconnect;
        //            port.PostMessage("Hello content!");
        //            break;
        //    }
        //}
        //void ContentPort_OnMessage(JSObject message)
        //{
        //    JS.Log("ContentPort_OnMessage", message);
        //}
        //void ContentPort_OnDisconnect(Port port)
        //{
        //    JS.Log("ContentPort_OnDisconnect", port);
        //}
        //void BackgroundPort_OnMessage(JSObject message)
        //{
        //    JS.Log("BackgroundPort_OnMessage", message);
        //}
        //void BackgroundPort_OnDisconnect(Port port)
        //{
        //    JS.Log("BackgroundPort_OnDisconnect", port);
        //}
    }
}
