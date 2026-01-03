using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Layouts;
using COMPASS.Common.ViewModels.Modals;
using COMPASS.Common.Views.Windows;
using System.Diagnostics;
using System.Reflection;
using Avalonia.Controls;
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
        private readonly IWebService _webService;
        private readonly IUIService _uiService;
        
        public MainViewModel()
        {
            _webService = ServiceResolver.Resolve<IWebService>();
            _uiService = ServiceResolver.Resolve<IUIService>();
            
            Logger.Init();
            InitLayouts();
            CollectionManager.DiscoverCollections();
            
            TabsVM = TabsViewModel.GetInstance();
            TabsVM.CreateTab();
            
            LeftDockVM = new(TabsVM);

            //check for update
            InitAutoUpdates();

            //Start timer that periodically checks if there is an internet connection
            InitConnectionTimer();
        }

        #region Init Functions

        private void InitAutoUpdates()
        {
            //TODO: this will all need to be replaced

            //            //Set URL of xml file
            //            AutoUpdater.AppCastURL = Constants.AutoUpdateXMLPath;
            //            //Disable skip
            //            AutoUpdater.ShowSkipButton = false;
            //            //Set Icon
            //            string? runningExePath = Process.GetCurrentProcess().MainModule?.FileName;
            //            if (!String.IsNullOrWhiteSpace(runningExePath))
            //            {
            //                AutoUpdater.Icon = System.Drawing.Icon.ExtractAssociatedIcon(runningExePath)?.ToBitmap();
            //            }
            //#if DEBUG
            //            //AutoUpdater.InstalledVersion = new("0.2.0"); //for testing only
            //#endif
            //            //set remind later time so users can go back to the app in one click
            //            AutoUpdater.LetUserSelectRemindLater = false;
            //            AutoUpdater.RemindLaterTimeSpan = RemindLaterFormat.Days;
            //            AutoUpdater.RemindLaterAt = 1;
            //            //Set download directory
            //            AutoUpdater.DownloadPath = Constants.InstallersPath;
            //            //check updates every 4 hours
            //            DispatcherTimer timer = new() { Interval = TimeSpan.FromHours(4) };
            //            timer.Tick += delegate
            //            {
            //                AutoUpdater.Mandatory = false;
            //                AutoUpdater.Start();
            //            };
            //            timer.Start();
            //            //check at startup
            //            AutoUpdater.Start();
        }

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

        //TODO reimplement this, should probably be moved out of settings viewmodel
        public RelayCommand CheckForUpdatesCommand => new(() => { }); //SettingsViewModel.CheckForUpdatesCommand;

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
