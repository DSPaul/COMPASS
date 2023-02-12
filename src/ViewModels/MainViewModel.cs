using AutoUpdaterDotNET;
using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.Windows;
using ImageMagick;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace COMPASS.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        public MainViewModel()
        {
            ViewModelBase.MVM = this;

            InitLogger();

            //Load everything
            LoadSettingsAndPreferences();
            LoadCollection();
            CurrentLayout = LayoutViewModel.GetLayout();

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

        private void LoadCollection()
        {
            Directory.CreateDirectory(CodexCollection.CollectionsPath);

            //Get all collections by folder name
            CollectionDirectories = new ObservableCollection<string>();
            string[] FullPathFolders = Directory.GetDirectories(CodexCollection.CollectionsPath);
            foreach (string p in FullPathFolders)
            {
                CollectionDirectories.Add(Path.GetFileName(p));
            }

            //in case of first boot, create default folder
            if (CollectionDirectories.Count == 0)
            {
                CreateCollection("Default");
            }

            //in case startup collection no longer exists, pick first one that does exists
            else if (!Directory.Exists(CodexCollection.CollectionsPath + Properties.Settings.Default.StartupCollection))
            {
                MessageBox.Show("The collection " + Properties.Settings.Default.StartupCollection + " could not be found. ");
                CurrentCollectionName = CollectionDirectories.First(dir => Directory.Exists(CodexCollection.CollectionsPath + dir));
            }

            //otherwise, open startup collection
            else
            {
                CurrentCollectionName = Properties.Settings.Default.StartupCollection;
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
        private ObservableCollection<string> _collectionDirectories;
        public ObservableCollection<string> CollectionDirectories
        {
            get => _collectionDirectories;
            set => SetProperty(ref _collectionDirectories, value);
        }

        private string _currentCollectionName;
        public string CurrentCollectionName
        {
            get => _currentCollectionName;
            set
            {
                if (CurrentCollection != null)
                {
                    CurrentCollection.SaveCodices();
                    CurrentCollection.SaveTags();
                }
                if (value != null) ChangeCollection(value);
                SetProperty(ref _currentCollectionName, value);
            }
        }

        private CodexCollection _currentCollection;
        public CodexCollection CurrentCollection
        {
            get => _currentCollection;
            private set => SetProperty(ref _currentCollection, value);
        }

        private bool _isOnline;
        public bool IsOnline
        {
            get => _isOnline;
            private set => SetProperty(ref _isOnline, value);
        }

        #endregion

        #region ViewModels

        //Settings ViewModel
        private SettingsViewModel _settingsVM;
        public SettingsViewModel SettingsVM
        {
            get => _settingsVM;
            set => SetProperty(ref _settingsVM, value);
        }

        private CollectionViewModel _collectionVM;
        public CollectionViewModel CollectionVM
        {
            get => _collectionVM;
            private set => SetProperty(ref _collectionVM, value);
        }

        private LayoutViewModel _currentLayout;
        public LayoutViewModel CurrentLayout
        {
            get => _currentLayout;
            set => SetProperty(ref _currentLayout, value);
        }

        private LeftDockViewModel _leftDockVM;
        public LeftDockViewModel LeftDockVM
        {
            get => _leftDockVM;
            set => SetProperty(ref _leftDockVM, value);
        }

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

        public void Refresh()
        {
            CollectionVM.ReFilter();
            LeftDockVM.TagsTabVM.RefreshTreeView();
        }

        //Change Collection
        public void ChangeCollection(string collectionDir)
        {
            CurrentCollection = new CodexCollection(collectionDir);
            CollectionVM = new CollectionViewModel(CurrentCollection.AllCodices);
            LeftDockVM = new LeftDockViewModel();
        }

        //Add new CodexCollection
        private RelayCommand<string> _createCollectionCommand;
        public RelayCommand<string> CreateCollectionCommand => _createCollectionCommand ??= new(CreateCollection);
        public void CreateCollection(string dirName)
        {
            if (string.IsNullOrEmpty(dirName)) return;

            Directory.CreateDirectory(CodexCollection.CollectionsPath + dirName + @"\CoverArt");
            Directory.CreateDirectory(CodexCollection.CollectionsPath + dirName + @"\Thumbnails");
            CollectionDirectories.Add(dirName);
            CurrentCollectionName = dirName;
        }

        //Rename Collection
        private RelayCommand<string> _editCollectionNameCommand;
        public RelayCommand<string> EditCollectionNameCommand => _editCollectionNameCommand ??= new(EditCollectionName);
        public void EditCollectionName(string newName)
        {
            var index = CollectionDirectories.IndexOf(CurrentCollectionName);
            CurrentCollection.RenameCollection(newName);
            CollectionDirectories[index] = newName;
            CurrentCollectionName = newName;
        }

        //Delete Collection
        private ActionCommand _deleteCollectionCommand;
        public ActionCommand DeleteCollectionCommand => _deleteCollectionCommand ??= new(RaiseDeleteCollectionWarning);
        public void RaiseDeleteCollectionWarning()
        {
            if (CurrentCollection.AllCodices.Count > 0)
            {
                //MessageBox "Are you Sure?"
                string sCaption = "Are you Sure?";

                string MessageSingle = "There is still one file in this collection, if you don't want to remove these from COMPASS, move them to another collection first. Are you sure you want to continue?";
                string MessageMultiple = $"There are still {CurrentCollection.AllCodices.Count} files in this collection, if you don't want to remove these from COMPASS, move them to another collection first. Are you sure you want to continue?";

                string sMessageBoxText = CurrentCollection.AllCodices.Count == 1 ? MessageSingle : MessageMultiple;

                MessageBoxButton btnMessageBox = MessageBoxButton.YesNo;
                MessageBoxImage imgMessageBox = MessageBoxImage.Warning;

                MessageBoxResult rsltMessageBox = MessageBox.Show(sMessageBoxText, sCaption, btnMessageBox, imgMessageBox);

                if (rsltMessageBox == MessageBoxResult.Yes)
                {
                    DeleteCollection(CurrentCollectionName);
                }
            }
            else
            {
                DeleteCollection(CurrentCollectionName);
            }
        }
        public void DeleteCollection(string todelete)
        {
            //if todelete is empty, it will delete the entire collections folder
            if (String.IsNullOrEmpty(todelete)) return;

            CollectionDirectories.Remove(todelete);
            CurrentCollectionName = CollectionDirectories[0];
            Directory.Delete(CodexCollection.CollectionsPath + todelete, true);
        }

        //Search
        private RelayCommand<string> _searchCommand;
        public RelayCommand<string> SearchCommand => _searchCommand ??= new(SearchCommandHelper);
        private void SearchCommandHelper(string Searchterm)
        {
            Filter SearchFilter = new(Filter.FilterType.Search, Searchterm)
            {
                Label = "Search:",
                BackgroundColor = Colors.Salmon,
                Unique = true
            };
            CollectionVM.AddFieldFilter(SearchFilter);
        }

        //called every few seconds to update IsOnline
        private void CheckConnection(object sender, EventArgs e) => IsOnline = Utils.PingURL();

        private ActionCommand _checkForUpdatesCommand;
        public ActionCommand CheckForUpdatesCommand => _checkForUpdatesCommand ??= new(CheckForUpdates);
        private void CheckForUpdates()
        {
            AutoUpdater.Mandatory = true;
            AutoUpdater.Start();
        }
        #endregion
    }
}
