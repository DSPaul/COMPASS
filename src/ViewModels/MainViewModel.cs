using AutoUpdaterDotNET;
using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.Windows;
using ImageMagick;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace COMPASS.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        public MainViewModel()
        {
            InitLogger();
            ViewModelBase.MVM = this;

            //Load everything
            UpgradeSettings();
            CollectionVM = new();
            CollectionVM.LoadInitialCollection();
            CurrentLayout = LayoutViewModel.GetLayout();
            LeftDockVM = new(this);
            CodexInfoVM = new(this);

            //Update stuff
            WebDriverFactory.UpdateWebdriver();
            InitAutoUpdates();

            //Start timer that periodically checks if there is an internet connection
            InitConnectionTimer();

            MagickNET.SetGhostscriptDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gs"));
        }

        #region Init Functions

        private void UpgradeSettings()
        {
            if (Properties.Settings.Default.justUpdated)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.justUpdated = false;
                Properties.Settings.Default.Save();
            }
        }

        private void InitAutoUpdates()
        {
            //Set URL of xml file
            AutoUpdater.AppCastURL = Constants.AutoUpdateXMLPath;
            //Disable skip
            AutoUpdater.ShowSkipButton = false;
            //AutoUpdater.InstalledVersion = new("0.2.0"); //for testing only
            //set remind later time so users can go back to the app in one click
            AutoUpdater.LetUserSelectRemindLater = false;
            AutoUpdater.RemindLaterTimeSpan = RemindLaterFormat.Days;
            AutoUpdater.RemindLaterAt = 1;
            //Set download directory
            AutoUpdater.DownloadPath = Constants.InstallersPath;
            //check updates every 4 hours
            DispatcherTimer timer = new() { Interval = TimeSpan.FromHours(4) };
            timer.Tick += delegate
            {
                AutoUpdater.Mandatory = false;
                AutoUpdater.Start();
            };
            timer.Start();
            //check at startup
            AutoUpdater.Start();
        }

        public void InitLogger()
        {
            log4net.GlobalContext.Properties["CompassDataPath"] = SettingsViewModel.CompassDataPath;
            Logger.FileLog = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            Application.Current.DispatcherUnhandledException += Logger.LogUnhandledException;
            Logger.Info($"Launching Compass v{Assembly.GetExecutingAssembly().GetName().Version.ToString()[0..5]}");
        }

        private void InitConnectionTimer()
        {
            //Start internet checkup timer
            _checkConnectionTimer = new();
            _checkConnectionTimer.Tick += (s, e) => Task.Run(() => IsOnline = Utils.PingURL());
            _checkConnectionTimer.Interval = new TimeSpan(0, 0, 10);
            _checkConnectionTimer.Start();
            //to check right away on startup
            Task.Run(() => IsOnline = Utils.PingURL());
        }

        #endregion

        #region Properties

        private bool _isOnline;
        public bool IsOnline
        {
            get => _isOnline;
            private set => SetProperty(ref _isOnline, value);
        }


        private DispatcherTimer _checkConnectionTimer;
        #endregion

        #region ViewModels

        public static CollectionViewModel CollectionVM { get; private set; }

        private LayoutViewModel _currentLayout;
        public LayoutViewModel CurrentLayout
        {
            get => _currentLayout;
            set => SetProperty(ref _currentLayout, value);
        }

        public LeftDockViewModel LeftDockVM { get; private set; }

        public CodexInfoViewModel CodexInfoVM { get; private set; }

        #endregion

        #region Commands and Methods

        //Open settings
        private RelayCommand<string> _openSettingsCommand;
        public RelayCommand<string> OpenSettingsCommand => _openSettingsCommand ??= new(OpenSettings);
        public void OpenSettings(string tab = null)
        {
            var settingswindow = new SettingsWindow(SettingsViewModel.GetInstance(), tab)
            {
                Owner = Application.Current.MainWindow
            };
            settingswindow.Show();
        }

        //check updates
        public ActionCommand CheckForUpdatesCommand => SettingsViewModel.GetInstance().CheckForUpdatesCommand;

        //Change Layout
        private RelayCommand<LayoutViewModel.Layout> _changeLayoutCommand;
        public RelayCommand<LayoutViewModel.Layout> ChangeLayoutCommand => _changeLayoutCommand ??= new(ChangeLayout);
        public void ChangeLayout(LayoutViewModel.Layout layout) => CurrentLayout = LayoutViewModel.GetLayout(layout);
        #endregion
    }
}
