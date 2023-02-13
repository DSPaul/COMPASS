using AutoUpdaterDotNET;
using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.Windows;
using ImageMagick;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace COMPASS.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        public MainViewModel()
        {
            InitLogger();

            //Load everything
            LoadSettingsAndPreferences();
            CollectionVM = new();
            CurrentLayout = LayoutViewModel.GetLayout();
            LeftDockVM = new(this);

            //Update stuff
            WebDriverFactory.UpdateWebdriver();
            InitAutoUpdates();

            //Start timer that periodically checks if there is an internet connection
            InitConnectionTimer();

            MagickNET.SetGhostscriptDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gs"));
        }

        #region Init Functions

        private void LoadSettingsAndPreferences()
        {
            SettingsVM = new SettingsViewModel();

            //migrate settings if first launch after update
            if (Properties.Settings.Default.justUpdated)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.justUpdated = false;
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
            Application.Current.DispatcherUnhandledException += Logger.LogUnhandledException;
            Logger.log.Info($"Launching Compass v{Assembly.GetExecutingAssembly().GetName().Version}");
        }

        private void InitConnectionTimer()
        {
            //Start internet checkup timer
            DispatcherTimer CheckConnectionTimer = new();
            CheckConnectionTimer.Tick += new EventHandler(CheckConnection);
            CheckConnectionTimer.Interval = new TimeSpan(0, 0, 30);
            CheckConnectionTimer.Start();
            //to check right away on startup
            IsOnline = Utils.PingURL();
        }

        #endregion

        #region Properties

        private bool _isOnline;
        public bool IsOnline
        {
            get => _isOnline;
            private set => SetProperty(ref _isOnline, value);
        }

        #endregion

        #region ViewModels

        public static CollectionViewModel CollectionVM { get; private set; }

        public static SettingsViewModel SettingsVM { get; private set; }

        private LayoutViewModel _currentLayout;
        public LayoutViewModel CurrentLayout
        {
            get => _currentLayout;
            set => SetProperty(ref _currentLayout, value);
        }

        public LeftDockViewModel LeftDockVM { get; private set; }

        #endregion

        #region Commands and Methods

        //Open settings
        private RelayCommand<string> _openSettingsCommand;
        public RelayCommand<string> OpenSettingsCommand => _openSettingsCommand ??= new(OpenSettings);
        public void OpenSettings(string tab = null)
        {
            var settingswindow = new SettingsWindow(SettingsVM, tab);
            settingswindow.Show();
        }

        //Change Layout
        private RelayCommand<LayoutViewModel.Layout> _changeLayoutCommand;
        public RelayCommand<LayoutViewModel.Layout> ChangeLayoutCommand => _changeLayoutCommand ??= new(ChangeLayout);
        public void ChangeLayout(LayoutViewModel.Layout layout) => CurrentLayout = LayoutViewModel.GetLayout(layout);

        //called every few seconds to update IsOnline
        private void CheckConnection(object sender, EventArgs e) => IsOnline = Utils.PingURL();
        #endregion
    }
}
