using Anaglyphohol.MultiviewMaker;
using Microsoft.AspNetCore.Components;
using SpawnDev.BlazorJS.BrowserExtension;
using SpawnDev.BlazorJS.BrowserExtension.Services;
using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Anaglyphohol.Layout
{
    public partial class ContentOverlay
    {
        [Parameter]
        [EditorRequired]
        public Assembly AppAssembly { get; set; }
        [Inject]
        ContentOverlayService ContentOverlayService { get; set; }
        StorageArea SyncStorage { get; set; }
        public bool HideContent => HideContentI == 0;
        int HideContentI { get; set; }
        [Parameter]
        public IEnumerable<Assembly> AdditionalAssemblies { get; set; }
        [Inject]
        AnaglyphImageMakerService AnaglyphImageMakerService { get; set; }
        [Inject] 
        BrowserExtensionService BrowserExtensionService { get; set; }
        public Dictionary<string, object> ContentParameters { get; set; } = new Dictionary<string, object>();
        Type? ContentType { get; set; }
        DynamicComponent? dynamicComponent = null;
        bool BeenInit = false;
        public List<ContentRouteInfo> ContentRouteInfos { get; } = new List<ContentRouteInfo>();
        public ContentOverlayRouteInfo? ContentOverlayRouteInfo { get; private set; }
        public bool Loading { get; private set; } = true;
        /// <summary>
        /// Loading progress from 0.0 - 100.0, or null if not available
        /// </summary>
        public float? LoadingProgress { get; private set; }
        public void SetLoading(float? progress = null)
        {
            progress = progress == null ? null : Math.Min(Math.Max(progress.Value, 0f), 100f);
            if (progress == 100f)
            {
                SetLoadingComplete();
                return;
            }
            if (Loading && LoadingProgress == progress)
            {
                return;
            }
            //Console.WriteLine($"SetLoading: {progress?.ToString() ?? "-"}");
            Loading = true;
            LoadingProgress = progress;
            StateHasChanged();
        }
        public void SetLoadingComplete()
        {
            if (!Loading) return;
            //Console.WriteLine($"SetLoadingComplete");
            Loading = false;
            LoadingProgress = null;
            StateHasChanged();
        }
        string[] ButtonIcons
        {
            get
            {
                switch (AnaglyphImageMakerService?.AnaglyphProfile ?? 0)
                {
                    case 0:
                        return new string[] { "red-blue-32.png", "arrows-rb-64.png" };
                    default:
                        return new string[] { "green-magenta-32.png", "arrows-gm-64.png" };
                }
            }
        }
        protected override void OnInitialized()
        {
            ContentOverlayService.ContentOverlay = this;
            if (!BeenInit)
            {
                SyncStorage = BrowserExtensionService.Browser!.Storage!.Sync;
                BeenInit = true;
                CacheRoutes();
                ContentOverlayUpdate();
                BrowserExtensionService.OnLocationChanged += BrowserExtensionService_OnLocationChanged;

                AnaglyphImageMakerService.OnStateHasChanged += AnaglyphImageMakerService_OnStateHasChanged;

            }
        }
        private void AnaglyphImageMakerService_OnStateHasChanged()
        {
            StateHasChanged();
        }
        protected override async Task OnInitializedAsync()
        {
            HideContentI = await SyncStorage.Get<int>($"{GetType().Name}_{nameof(HideContent)}", 1);
        }
        async Task Clicked(int index)
        {
            HideContentI = index;
            await SyncStorage.Set($"{GetType().Name}_{nameof(HideContent)}", HideContentI);
        }
        private void BrowserExtensionService_OnLocationChanged(Uri obj)
        {
            ContentOverlayUpdate();
        }
        void ContentOverlayUpdate()
        {
            var routeInfo = GetBestContentComponentRoute(BrowserExtensionService.Location);
            ContentOverlayRouteInfo = routeInfo;
            var contentType = routeInfo?.ContentComponentType;
            if (ContentType != contentType)
            {
                ContentType = contentType;
#if DEBUG && false
                Console.WriteLine($"ContentType changed: {ContentType?.Name ?? "NONE"}");
#endif
                StateHasChanged();
            }
        }
        void CacheRoutes()
        {
            var routes = new List<ContentOverlayRouteInfo>();
            var assemblies = new List<Assembly> { AppAssembly };
            if (AdditionalAssemblies != null) assemblies.AddRange(AdditionalAssemblies);
            foreach (var assembly in assemblies)
            {
                var componentTypes = assembly.ExportedTypes.Where(o => o.IsSubclassOf(typeof(ComponentBase))).ToList();
                foreach (var componentType in componentTypes)
                {
                    var attrs = componentType.GetCustomAttributes<ContentLocationAttribute>();
                    attrs = attrs.OrderByDescending(o => o.Weight).ThenByDescending(o => o.LocationRegexPattern.Length).ToList();
                    if (attrs.Count() == 0) continue;
                    var contentRouteInfo = new ContentRouteInfo
                    {
                        Assembly = assembly,
                        ComponentType = componentType,
                        ContentLocations = (List<ContentLocationAttribute>)attrs,
                    };
                    ContentRouteInfos.Add(contentRouteInfo);
                }
            }
        }
        ContentOverlayRouteInfo? GetBestContentComponentRoute(string location)
        {
            var routes = new List<ContentOverlayRouteInfo>();
            foreach (var contentRouteInfo in ContentRouteInfos)
            {
                foreach (var attr in contentRouteInfo.ContentLocations)
                {
                    var m = Regex.Match(location, attr.LocationRegexPattern);
                    if (m.Success)
                    {
                        routes.Add(new ContentOverlayRouteInfo(location, contentRouteInfo.ComponentType, m, attr));
                        break;
                    }
                }
            }
            // sort by weight first, and then pattern length; then take the first
            var ret = routes.OrderByDescending(o => o.ContentLocationAttribute.Weight).ThenByDescending(o => o.ContentLocationAttribute.LocationRegexPattern.Length).FirstOrDefault();
            return ret;
        }
    }
}
