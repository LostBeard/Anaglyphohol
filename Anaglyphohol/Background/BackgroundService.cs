using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.BrowserExtension;
using SpawnDev.BlazorJS.BrowserExtension.Services;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.WebWorkers;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace Anaglyphohol.Background
{
    public class BackgroundService : ServiceWorkerEventHandler
    {
        BrowserExtensionService BrowserExtensionService;
        List<Task>? InitWaitFor = new List<Task>();

        DeclarativeNetRequest? DeclarativeNetRequest => BrowserExtensionService.Browser?.DeclarativeNetRequest;
        Tabs? Tabs => BrowserExtensionService.Browser?.Tabs;
        public bool InBackground { get; }
        /// <summary>
        /// If enabled, csp issues on Blazor boot up in content scripts will be fixed using a DeclarativeNetRequest rule that patches the response headers for the given page<br/>
        /// THat wya the page can be reloaded and Blazor can load<br/>
        /// This requires "declarativeNetRequest" in the permissions<br/>
        /// This is only needed for some sites like github.com
        /// </summary>
        public bool AutoFixExtensionCSP { get; set; } = false;
        public BackgroundService(BlazorJSRuntime js, BrowserExtensionService browserExtensionService) : base(js)
        {
            BrowserExtensionService = browserExtensionService;
            InBackground = BrowserExtensionService.ExtensionMode == ExtensionMode.Background;
            if (InBackground)
            {
                BrowserExtensionService.Browser!.Runtime!.OnMessage += Runtime_OnMessage;
#if DEBUG
                DeclarativeNetRequest!.OnRuleMatchedDebug += OnRuleMatchedDebug;
#endif
                Tabs!.OnUpdated += Tabs_OnUpdated;

                //using var contextMenus = BrowserExtensionService.Browser.ContextMenus;
                //contextMenus.OnClicked += ContextMenus_OnClicked;


            }
        }
        void ContextMenus_OnClicked(OnClickData onClickData, Tab tab)
        {
            JS.Log("ContextMenus_OnClicked", onClickData, tab);
            if (onClickData.MenuItemId == "view_anaglyph")
            {
                var srcUrl = onClickData.SrcUrl;
                JS.Log("srcUrl", srcUrl);
                if (!string.IsNullOrEmpty(srcUrl))
                {
                    var viewerUrl = BrowserExtensionService.GetURL($"index.html?$=viewer&imageUrl={Uri.EscapeDataString(srcUrl)}");
                    JS.Log("viewerUrl", viewerUrl);
                    Tabs!.Create(new CreateTabProperties
                    {
                        Url = viewerUrl,
                        Active = true,

                    });
                }
            }
        }
        void Tabs_OnUpdated(ChangeInfo info)
        {
            //JS.Log("Tabs_OnUpdated .Net", info);
        }
        void OnRuleMatchedDebug(MatchedRuleInfo info)
        {
            Console.WriteLine($"OnRuleMatchedDebug: {info.Rule.RuleId}");
        }
        public void InitAsyncWaitFor(Task task)
        {
            if (InitWaitFor == null) throw new Exception("InitWaitFor is null. BackgroundWorker.OnInitializedAsync has already completed.");
            if (InitWaitFor.Contains(task)) return;
            InitWaitFor.Add(task);
        }
        protected override async Task OnInitializedAsync()
        {
            Log("ExtensionServiceWorker InitAsync >>");
            // little delay to let other auto-starting services run
            if (InBackground)
            {
#if DEBUG
                var rules = await DeclarativeNetRequest!.GetDynamicRules();
                JS.Log("Rules::", rules);
#endif

                //await AddContextMenuItems();

                //await AddAppRedirectRule();
            }
            await Task.Delay(50);
            await Task.WhenAll(InitWaitFor!);
            InitWaitFor = null;
            Log("ExtensionServiceWorker InitAsync <<");
        }
        async Task AddContextMenuItems()
        {
            JS.Log("Creating context menus");
            using var contextMenus = BrowserExtensionService.Browser!.ContextMenus;
            // TODO check before adding... may have already been added
            // usually just added on install, but not sure if we miss that event while Blazor is loading (not async app friendly event)
            using var id = contextMenus.Create(new CreateProperties
            {
                Title = "View Anaglyph Image",
                Id = "view_anaglyph",
                Contexts = new[] { "image" },
            });
        }
        bool Runtime_OnMessage(JSObject data, MessageSender sender, Function? sendResponse)
        {
#if DEBUG
            JS.Log("bg...Runtime_OnMessage *****");
#endif
            var cspViolation = data.JSRef!.TypeOf() == "object" ? data.JSRef!.Get<CSPViolation?>("cspViolation") : null;
            if (cspViolation != null)
            {
                _ = PatchCSP(cspViolation, sender);
            }
            return false;
        }
        int AppRedirectRuleId = 1234567;
        async Task DelAppRedirectRule()
        {
            await DeclarativeNetRequest!.UpdateSessionRules(new UpdateRuleOptions
            {
                RemoveRuleIds = new[] { AppRedirectRuleId }
            });
        }
        async Task AddAppRedirectRule()
        {
            await DelAppRedirectRule();
            var extensionId = BrowserExtensionService.ExtensionId;
            var extensionRootPath = $"chrome-extension://{extensionId}";

            //var extensionBasePath = $"{extensionRootPath}/app/index.html?page=\x01";
            //var pageUrlEscaped = Regex.Escape(pageUrl);
            var cspRule = new Rule
            {
                Id = AppRedirectRuleId,
                Action = new RuleAction
                {
                    Type = RuleActionType.Redirect,
                    Redirect = new Redirect
                    {
                        RegexSubstitution = $"{extensionRootPath}/app/index.html?page=\x01",
                    },
                },
                Condition = new RuleCondition
                {
                    //RegexFilter = $@"^{extensionRootPath}(/.*)?$",
                    RegexFilter = "chrome-extension://faoljfandgephiloengedfcipoojfoji/app/(SystemInfo)",
                    ResourceTypes = new EnumString<ResourceType>[] { ResourceType.MainFrame },
                }
            };
            JS.Log("Adding AddAppRedirectRule", cspRule);
            // save rule
            await DeclarativeNetRequest!.UpdateSessionRules(new UpdateRuleOptions
            {
                AddRules = new Rule[] { cspRule },
            });
        }
        async Task PatchCSP(CSPViolation cspViolation, MessageSender sender)
        {
            var originalPolicy = cspViolation.OriginalPolicy;
            var updatedPolicy = originalPolicy;
            if (originalPolicy.IndexOf("wasm-unsafe-eval") == -1)
            {
                updatedPolicy = originalPolicy.Replace("script-src ", "script-src 'wasm-unsafe-eval' ");
            }
            else
            {
                // rule already has 'wasm-unsafe-eval'
                // if this happens, there is another problem
                return;
            }
            var url = new Uri(cspViolation.DocumentURI);
            // separate paths on the same domain MAY (uncommon) have different csp rules
            // the query string and hash shouldn't have any effect on csp rules
            // check if the extension actualyl wants to patch this csp or not
            // could check user settings.
            var shouldPatchCSP = await ShouldPatchCSPCheck(url);
            if (!shouldPatchCSP)
            {
                // ignore the csp issue
                return;
            }
            var pageUrl = url.GetLeftPart(UriPartial.Path);
            var pageUrlEscaped = Regex.Escape(pageUrl);
            var ruleId = await GetFreeSessionRuleId();
            var cspRule = new Rule
            {
                Id = ruleId,
                Action = new RuleAction
                {
                    Type = RuleActionType.ModifyHeaders,
                    ResponseHeaders = new ModifyHeaderInfo[]
                    {
                        new ModifyHeaderInfo
                        {
                            Header = "content-security-policy",
                            Operation = HeaderOperation.Set,
                            Value = updatedPolicy,
                        }
                    },
                },
                Condition = new RuleCondition
                {
                    RegexFilter = $@"^{pageUrlEscaped}(\?.*)?(#.*)?$",
                    ResourceTypes = new EnumString<ResourceType>[] { ResourceType.MainFrame, ResourceType.SubFrame, ResourceType.XMLHttpRequest },
                }
            };
            JS.Log("Adding rule", pageUrl, cspRule);
            // save rule
            await DeclarativeNetRequest!.UpdateSessionRules(new UpdateRuleOptions
            {
                AddRules = new Rule[] { cspRule },
            });
            // reload tab so the new rule can take effect
            await BrowserExtensionService.Browser!.Tabs!.Reload(sender.Tab!.Id);
        }
        async Task<bool> ShouldPatchCSPCheck(Uri url)
        {
            return AutoFixExtensionCSP;
        }
        async Task<int> GetFreeSessionRuleId()
        {
            using var rules = await DeclarativeNetRequest!.GetSessionRules();
            var ruleIds = rules.ToArray().Select(o => o.Id).ToArray();
            var id = Random.Shared.Next(0, int.MaxValue);
            while (ruleIds.Contains(id))
            {
                id = Random.Shared.Next(0, int.MaxValue);
            }
            return id;
        }
        void Log(params object[] args)
        {
            //JS.Log(new object?[] { $"ServiceWorkerEventHandler > {JS.InstanceId}" }.Concat(args).ToArray());
        }
        protected override async Task ServiceWorker_OnInstallAsync(ExtendableEvent e)
        {
            Log($"self.SkipWaiting()");
            await ServiceWorkerThis!.SkipWaiting();
        }
        protected override async Task ServiceWorker_OnActivateAsync(ExtendableEvent e)
        {
            Log($"clients.Claim()");
            using var clients = ServiceWorkerThis!.Clients;
            await clients.Claim();
        }
    }
}
