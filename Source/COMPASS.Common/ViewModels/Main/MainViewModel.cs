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
using COMPASS.Common.Services;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Infra.Tools;

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

        private void InitLayouts() => AllLayouts = Assembly.GetExecutingAssembly()
                                 .GetTypes()
                                 .Where(t => !t.IsAbstract && typeof(LayoutViewModel).IsAssignableFrom(t))
                                 .Select(Activator.CreateInstance)
                                 .OfType<LayoutViewModel>()
                                 .OrderBy(l => l.LayoutType)
                                 .ToList();

        #endregion

        #region Properties

        public GridLength WindowControlsSpacing => _uiService.WindowControlsSpacing;
        
        public static bool SaveOnClose { get; set; } = true;

        private bool _isOnline;
        public bool IsOnline
        {
            get => _isOnline;
            private set => SetProperty(ref _isOnline, value);
        }

        public string VersionName => $"v{ApplicationService.Version}";
        public ProgressViewModel ProgressVM => ProgressViewModel.GetInstance();

        #endregion

        #region ViewModels
        
        public TabsViewModel TabsVM { get; }

        public IList<LayoutViewModel> AllLayouts { get; private set; } = [];

        public LeftDockViewModel LeftDockVM { get; init; }

        #endregion

        #region Commands and Methods

        //Open settings
        private RelayCommand<string>? _openSettingsCommand;
        public RelayCommand<string> OpenSettingsCommand => _openSettingsCommand ??= new(OpenSettings);
        private void OpenSettings(string? tab = "")
        {
            var settingsWindow = new ModalWindow(new SettingsViewModel(tab ?? ""));
            settingsWindow.Show(App.MainWindow);
        }

        //TODO reimplement this, should probably be moved out of settings viewmodel
        public RelayCommand CheckForUpdatesCommand => new(() => { }); //SettingsViewModel.CheckForUpdatesCommand;

        private RelayCommand<string>? _navigateToCommand;
        public RelayCommand<string> NavigateToCommand => _navigateToCommand ??= new(url =>
        {
            if (string.IsNullOrEmpty(url)) return;
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        });

        private RelayCommand? _navigateToLinkTree;
        public RelayCommand NavigateToLinkTree => _navigateToLinkTree ??= new(()
            => Process.Start(new ProcessStartInfo(@"https://linktr.ee/compassapp") { UseShellExecute = true }));

        private RelayCommand? _navigateToKofi;
        public RelayCommand NavigateToKofi => _navigateToKofi ??= new(()
            => Process.Start(new ProcessStartInfo(@"https://ko-fi.com/pauldesmul") { UseShellExecute = true }));
        
        #endregion
    }
}
