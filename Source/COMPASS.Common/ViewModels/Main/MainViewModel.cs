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
using Material.Icons;

namespace COMPASS.Common.ViewModels.Main
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ILogger _logger;
        private readonly IUIService _uiService;
        private readonly UpdateManager _updateManager;
        private readonly SettingsViewModelFactory _settingsViewModelFactory;

        public MainViewModel(ILogger logger, IUIService uiService, UpdateManager updateManager, TabsViewModel tabsVm,
            LeftDockViewModelFactory leftDockViewModelFactory,
            SettingsViewModelFactory settingsViewModelFactory)
        {
            _logger = logger;
            _uiService = uiService;
            _updateManager = updateManager;
            _settingsViewModelFactory = settingsViewModelFactory;

            _logger.Info($"Launching COMPASS v{ApplicationService.Version}");

            InitLayouts();
            CollectionManager.DiscoverCollections();

            TabsVM = tabsVm;
            TabsVM.TabCreated += TabsVM_TabCreated;
            TabsVM.CreateTab();

            LeftDockVM = leftDockViewModelFactory.Create(TabsVM);

            InitCheckForUpdates();

            InitConnectionTimer();
        }

        private void TabsVM_TabCreated(object? sender, CollectionTabVM e)
        {
            //Fire and forget auto import on that new tab
            _ = e.CollectionVM.AutoImport();
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
            _updateManager.OnUpdateFound += (_, _) => UpdateAvailable = _updateManager.UpdatesAvailable;
            _updateManager.StartUpdateCheckLoop();
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
            var settingsVm = _settingsViewModelFactory.Create(tab ?? "");
            var settingsWindow = new ModalWindow(settingsVm);
            settingsWindow.Show(WindowManager.ActiveWindow);
        }

        private AsyncRelayCommand? _checkForUpdatesCommand;
        public AsyncRelayCommand CheckForUpdatesCommand => _checkForUpdatesCommand ??= new(CheckForUpdates);
        private async Task CheckForUpdates()
        {
            await _updateManager.ExplicitCheckUpdates();
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
