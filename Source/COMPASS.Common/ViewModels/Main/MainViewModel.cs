using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.ViewModels.Modals;
using COMPASS.Common.Views.Windows;
using System.Diagnostics;
using Avalonia.Controls;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Infra.Tools;
using Material.Icons;
using COMPASS.Infra.Interfaces.Services;

namespace COMPASS.Common.ViewModels.Main
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IWebService _webService;
        private readonly IUIService _uiService;
        private readonly IUpdateService _updateService;
        
        public MainViewModel()
        {
            Logger.Info($"Launching COMPASS v{ApplicationService.Version}");

            _webService = ServiceResolver.Resolve<IWebService>();
            _uiService = ServiceResolver.Resolve<IUIService>();
            _updateService = ServiceResolver.Resolve<IUpdateService>();
            
            InitLayouts();
            CollectionManager.DiscoverCollections();
            
            TabsVM = TabsViewModel.GetInstance();
            TabsVM.CreateTab();
            
            LeftDockVM = new(TabsVM);

            //check for updates
            UpdateManager.Run(_updateService);

            //Start timer that periodically checks if there is an internet connection
            InitConnectionTimer();
        }

        #region Init Functions

        private void InitConnectionTimer()
        {
            //Start internet checkup timer
            DispatcherTimer checkConnectionTimer = new();
            checkConnectionTimer.Tick += (_, _) => Task.Run(() => IsOnline = _webService.CheckConnection());
            checkConnectionTimer.Interval = new TimeSpan(0, 0, 10);
            checkConnectionTimer.Start();
            //to check right away on startup
            Task.Run(() => IsOnline = _webService.CheckConnection());
        }

        private void InitLayouts() => AllLayouts = 
            [
                new(CodexLayout.Home, "Home", MaterialIconKind.Home),
                new(CodexLayout.List, "List", MaterialIconKind.ViewHeadline),
                new(CodexLayout.Card, "Card", MaterialIconKind.FormatListText),
                new(CodexLayout.Tile, "Tile", MaterialIconKind.ViewGrid),
            ];

        #endregion

        #region Properties

        public GridLength WindowControlsSpacing => _uiService.WindowControlsSpacing;
        
        public static bool SaveOnClose { get; set; } = true;

        public bool IsOnline
        {
            get;
            private set => SetProperty(ref field, value);
        }

        public string VersionName => $"v{ApplicationService.Version}";
        public ProgressViewModel ProgressVM => ProgressViewModel.GetInstance();

        #endregion

        #region ViewModels
        
        public TabsViewModel TabsVM { get; }

        public IList<Layout> AllLayouts { get; private set; } = [];

        public LeftDockViewModel LeftDockVM { get; init; }

        #endregion

        #region Commands and Methods

        //Open settings
        public RelayCommand<string> OpenSettingsCommand => field ??= new(OpenSettings);
        private void OpenSettings(string? tab = "")
        {
            var settingsVm = new SettingsViewModel(tab ?? "");
            var settingsWindow = new ModalWindow(settingsVm);
            settingsWindow.Show(WindowManager.ActiveWindow);
        }

        private AsyncRelayCommand? _checkForUpdatesCommand;
        public AsyncRelayCommand CheckForUpdatesCommand => _checkForUpdatesCommand ??= new(UpdateManager.CheckForUpdates);

        public RelayCommand<string> NavigateToCommand => field ??= new(url =>
        {
            if (string.IsNullOrEmpty(url)) return;
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        });

        public RelayCommand NavigateToLinkTree => field ??= new(()
            => Process.Start(new ProcessStartInfo(@"https://linktr.ee/compassapp") { UseShellExecute = true }));

        public RelayCommand NavigateToKofi => field ??= new(()
            => Process.Start(new ProcessStartInfo(@"https://ko-fi.com/pauldesmul") { UseShellExecute = true }));
        
        #endregion
    }
}
