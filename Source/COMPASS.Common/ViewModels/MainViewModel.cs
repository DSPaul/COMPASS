using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Layouts;
using COMPASS.Common.ViewModels.Modals;
using COMPASS.Common.Views.Windows;
using ImageMagick;

namespace COMPASS.Common.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            Logger.Init();
            ViewModelBase.MVM = this;

            InitLayouts();
            
            //Load everything
            CollectionVM = new(this);
            _currentLayout = LayoutViewModel.GetLayout();
            LeftDockVM = new(this);

            //Update stuff
            InitAutoUpdates();

            //Start timer that periodically checks if there is an internet connection
            InitConnectionTimer();

            MagickNET.SetGhostscriptDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gs"));
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
            checkConnectionTimer.Tick += (_, _) => Task.Run(() => IsOnline = IOService.PingURL());
            checkConnectionTimer.Interval = new TimeSpan(0, 0, 10);
            checkConnectionTimer.Start();
            //to check right away on startup
            Task.Run(() => IsOnline = IOService.PingURL());
        }

        private void InitLayouts()
        {
            AllLayouts = Assembly.GetExecutingAssembly()
                                 .GetTypes()
                                 .Where(t => !t.IsAbstract && typeof(LayoutViewModel).IsAssignableFrom(t))
                                 .Select(t => Activator.CreateInstance(t))
                                 .OfType<LayoutViewModel>()
                                 .OrderBy(l => l!.LayoutType)
                                 .ToList();
        }

        #endregion

        #region Properties

        private bool _isOnline;
        public bool IsOnline
        {
            get => _isOnline;
            private set => SetProperty(ref _isOnline, value);
        }

        public string VersionName => $"v{Reflection.Version}";
        public ProgressViewModel ProgressVM => ProgressViewModel.GetInstance();

        #endregion

        #region ViewModels

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public static CollectionViewModel CollectionVM { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public CollectionViewModel BindableCollectionVM => CollectionVM; //because binding to static properties sucks
        
        private LayoutViewModel _currentLayout;
        public LayoutViewModel CurrentLayout
        {
            get => _currentLayout;
            set => SetProperty(ref _currentLayout, value);
        }

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

        //Change Layout
        private RelayCommand<CodexLayout>? _changeLayoutCommand;
        public RelayCommand<CodexLayout> ChangeLayoutCommand => _changeLayoutCommand ??= new(ChangeLayout);
        public void ChangeLayout(CodexLayout layout) => CurrentLayout = LayoutViewModel.GetLayout(layout);
        #endregion
    }
}
