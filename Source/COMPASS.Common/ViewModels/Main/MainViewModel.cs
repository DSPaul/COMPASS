using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.ViewModels.Modals;
using COMPASS.Common.Views.Windows;
using System.Diagnostics;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Infra.Tools;
using Material.Icons;

namespace COMPASS.Common.ViewModels.Main
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IUIService _uiService;
        
        public MainViewModel()
        {
            Logger.Info($"Launching COMPASS v{ApplicationService.Version}");

            _uiService = ServiceResolver.Resolve<IUIService>();

            InitLayouts();
            CollectionManager.DiscoverCollections();

            TabsVM = TabsViewModel.GetInstance();
            TabsVM.CreateTab();

            LeftDockVM = new(TabsVM);

            InitCheckForUpdates();

            InitConnectionTimer();
        }


        #region Init Functions
        /// <summary>
        /// Start timer that periodically checks if there is an internet connection
        /// </summary>
        private void InitConnectionTimer()
        {
            ConnectivityManager.IsOnlineChanged += online => IsOnline = online;
            ConnectivityManager.Start();
        }

        /// <summary>
        /// Start the periodic update checking loop
        /// </summary>
        private void InitCheckForUpdates()
        {
            var updateManager = ServiceResolver.Resolve<UpdateManager>();
            updateManager.OnUpdateFound += (_, _) => UpdateAvailable = updateManager.UpdatesAvailable;
            updateManager.StartUpdateCheckLoop();
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
        } = true;

        public string VersionName => $"v{ApplicationService.Version}";
        public bool UpdateAvailable { get; set => SetProperty(ref field, value); }
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
        public AsyncRelayCommand CheckForUpdatesCommand => _checkForUpdatesCommand ??= new(CheckForUpdates);
        private async Task CheckForUpdates()
        {
            var updateManager = ServiceResolver.Resolve<UpdateManager>();
            await updateManager.ExplicitCheckUpdates();
        }

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
