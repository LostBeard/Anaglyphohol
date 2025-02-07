using Anaglyphohol.Layout;
using Anaglyphohol.MultiviewMaker;
using Anaglyphohol.MultiviewMaker.Renderers;
using Anaglyphohol.Services;
using Microsoft.AspNetCore.Components;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.BrowserExtension;
using SpawnDev.BlazorJS.BrowserExtension.Services;
using SpawnDev.BlazorJS.JSObjects;
using Window = SpawnDev.BlazorJS.JSObjects.Window;

namespace Anaglyphohol.ExtensionContent
{
    public partial class DefaultOverlay : IDisposable
    {
        [Inject]
        ContentOverlayService ContentOverlayService { get; set; } = default!;

        [Inject]
        BlazorJSRuntime JS { get; set; } = default!;

        [Inject]
        BrowserExtensionService BrowserExtensionService { get; set; } = default!;

        [Inject]
        ContentBridgeService ContentBridge { get; set; } = default!;

        [Inject]
        DepthEstimationService DepthEstimationService { get; set; } = default!;

        [Inject]
        AnaglyphImageMakerService AnaglyphImageMakerService { get; set; } = default!;

        [Inject]
        ImageTracker ImageTracker { get; set; } = default!;

        StorageArea SyncStorage { get; set; }
        int AutoAnaglyphAll { get; set; }
        int AnaglyphProfile { get; set; }
        Document Document { get; set; }
        Window Window { get; set; }
        bool beenInit = false;
        bool initComplete = false;
        Screen Screen { get; set; }

        string AnaglyphAllFlag = $"DefaultOverlay_AutoAnaglyphAll";
        string AnaglyphProfileFlag = "DefaultOverlay_AnaglyphProfile";

        protected override async Task OnInitializedAsync()
        {
            if (beenInit) return;
            beenInit = true;
            SyncStorage = BrowserExtensionService.Browser!.Storage!.Sync;
            Document = JS.Get<Document>("document");
            Window = JS.Get<Window>("window");
            Screen = Window.Screen;
            AutoAnaglyphAll = await SyncStorage.Get<int>(AnaglyphAllFlag);
            AnaglyphProfile = await SyncStorage.Get<int>(AnaglyphProfileFlag);
            ImageTracker.AutoAnaglyphAll = AutoAnaglyphAll == 1;
            ImageTracker.AnaglyphProfile = AnaglyphProfile;
            ImageTracker.OnStateChanged += ImageTracker_OnStateChanged;

            DepthEstimationService.OnStateChange += DepthEstimationService_OnStateChange;

            ImageTracker.Start();
            initComplete = true;
            //ContentOverlayService.ContentOverlay.SetLoadingComplete();
            UpdateContentProgress();
            StateHasChanged();
        }

        private void DepthEstimationService_OnStateChange()
        {
            UpdateContentProgress();
        }
        void UpdateContentProgress()
        {
            if (DepthEstimationService.Loading)
            {
               // Console.WriteLine("UpdateContentProgress 0");
                ContentOverlayService.ContentOverlay.SetLoading(DepthEstimationService.OverallLoadProgress);
            }
            else if (ImageTracker.IsBusy)
            {
               /// Console.WriteLine("UpdateContentProgress 1");
                ContentOverlayService.ContentOverlay.SetLoading(ImageTracker.Progress);
            }
            else
            {
                //Console.WriteLine("UpdateContentProgress 2");
                ContentOverlayService.ContentOverlay.SetLoadingComplete();
            }
        }

        private void ImageTracker_OnStateChanged()
        {
            UpdateContentProgress();
            StateHasChanged();
        }

        async Task AutoAnaglyphAll_OnClicked(int index)
        {
            AutoAnaglyphAll = index;
            await SyncStorage.Set(AnaglyphAllFlag, AutoAnaglyphAll);
            // handle change
            ImageTracker.AutoAnaglyphAll = AutoAnaglyphAll == 1;
            StateHasChanged();
        }
        async Task AnaglyphProfile_OnClicked(int index)
        {
            AnaglyphProfile = index;
            //Console.WriteLine($"AnaglyphProfile: {AnaglyphProfile}");
            await SyncStorage.Set(AnaglyphProfileFlag, AnaglyphProfile);
            // handle change
            ImageTracker.AnaglyphProfile = AnaglyphProfile;
            StateHasChanged();
        }
        public void Dispose()
        {
            Console.WriteLine($"{GetType().Name}.Dispose");
            ImageTracker.OnStateChanged -= ImageTracker_OnStateChanged;
            DepthEstimationService.OnStateChange -= DepthEstimationService_OnStateChange;
            ImageTracker.Dispose();
        }
    }
}
