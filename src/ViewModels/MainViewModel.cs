using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.ViewModels.Commands;
using COMPASS.Windows;
using ImageMagick;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using static COMPASS.Tools.Enums;
using AutoUpdaterDotNET;
using System.Reflection;

namespace COMPASS.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        public MainViewModel()
        {
            ViewModelBase.MVM = this;
            SettingsVM = new SettingsViewModel();

            //init Logger
            InitLogger();

            //Load data
            InitCollection();

            //Get webdriver for Selenium
            InitWebdriver();

            //Start timer that periodically checks if there is an internet connection
            InitConnectionTimer();

            //check for updates
            InitAutoUpdates();

            //do stuff if first launch after update
            if (Properties.Settings.Default.justUpdated)
            {
                FirstLaunch();
                Properties.Settings.Default.justUpdated = false;
            }

            MagickNET.SetGhostscriptDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"gs"));

            //Commands
            ChangeFileViewCommand = new RelayCommand<CodexLayout>(ChangeFileView);
            ResetCommand = new ActionCommand(Reset);
            AddTagCommand = new ActionCommand(AddTag);
            ImportFilesCommand = new RelayCommand<Sources>(ImportFiles);
            CreateCollectionCommand = new RelayCommand<string>(CreateCollection);
            EditCollectionNameCommand = new RelayCommand<string>(EditCollectionName);
            DeleteCollectionCommand = new ActionCommand(RaiseDeleteCollectionWarning);
            OpenSettingsCommand = new RelayCommand<string>(OpenSettings);
            CheckForUpdatesCommand = new ActionCommand(CheckForUpdates);
        }

        #region Init Functions

        //Get a collection at startup
        private void InitCollection()
        {
            Directory.CreateDirectory(CodexCollection.CollectionsPath);

            //Get all RPG systems by folder name
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

            //in case startup collection no longer exists
            else if (!Directory.Exists(CodexCollection.CollectionsPath + Properties.Settings.Default.StartupCollection))
            {
                MessageBox.Show("The collection " + Properties.Settings.Default.StartupCollection + " could not be found. ");
                //pick first one that does exists
                foreach (string dir in CollectionDirectories)
                {
                    if (Directory.Exists(CodexCollection.CollectionsPath + dir))
                    {
                        CurrentCollectionName = dir;
                        break;
                    }
                }
            }

            //otherwise, open startup collection
            else
            {
                CurrentCollectionName = Properties.Settings.Default.StartupCollection;
            }
        }

        //Get latest version of relevant Webdriver for selenium
        private void InitWebdriver()
        {
            Directory.CreateDirectory(Constants.WebDriverDirectoryPath);
            DriverManager DM = new(Constants.WebDriverDirectoryPath);

            if (Utils.IsInstalled("chrome.exe"))
            {
                Properties.Settings.Default.SeleniumBrowser = (int)Browser.Chrome;
                try
                {
                    DM.SetUpDriver(new ChromeConfig(), WebDriverManager.Helpers.VersionResolveStrategy.MatchingBrowser);
                }
                catch (Exception ex)
                {
                    Logger.log.Error(ex.Message);
                }
            }

            else if (Utils.IsInstalled("firefox.exe"))
            {
                Properties.Settings.Default.SeleniumBrowser = (int)Browser.Firefox;
                try
                {
                    DM.SetUpDriver(new FirefoxConfig());
                }
                catch (Exception ex)
                {
                    Logger.log.Error(ex.Message);
                }
            }

            else
            {
                Properties.Settings.Default.SeleniumBrowser = (int)Browser.Edge;
                try
                {
                    DM.SetUpDriver(new EdgeConfig(), WebDriverManager.Helpers.VersionResolveStrategy.MatchingBrowser);
                }
                catch (Exception ex)
                {
                    Logger.log.Error(ex.Message);
                }
            }
        }
        
        private void InitAutoUpdates()
        {
            //Disable skip
            AutoUpdater.ShowSkipButton = false;
            AutoUpdater.InstalledVersion = new("0.5.0");
            //set remind later time so users can go back to the app in one click
            AutoUpdater.LetUserSelectRemindLater = false;
            AutoUpdater.RemindLaterTimeSpan = RemindLaterFormat.Days;
            AutoUpdater.RemindLaterAt = 1;
            //Set download directory
            AutoUpdater.DownloadPath = Constants.InstallersPath;
            //check updates every 4 hours
            DispatcherTimer timer = new(){ Interval = TimeSpan.FromHours(4) };
            timer.Tick += delegate
            {
                CheckForUpdates();
            };
            timer.Start();
            //check at startup
            CheckForUpdates();
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

        private void FirstLaunch()
        {
            Properties.Settings.Default.Upgrade();
        }
        #endregion

        #region Properties
        private ObservableCollection<string> _collectionDirectories;
        public ObservableCollection<string> CollectionDirectories
        {
            get { return _collectionDirectories; }
            set { SetProperty(ref _collectionDirectories, value); }
        }

        private string _currentCollectionName;
        public string CurrentCollectionName
        {
            get { return _currentCollectionName; }
            set
            {
                if (CurrentCollection != null)
                {
                    CurrentCollection.SaveCodices();
                    CurrentCollection.SaveTags();
                }
                if(value != null) ChangeCollection(value);
                SetProperty(ref _currentCollectionName, value);
            }
        }

        //CodexCollection 
        private CodexCollection _currentCollection;
        public CodexCollection CurrentCollection
        {
            get { return _currentCollection; }
            private set { SetProperty(ref _currentCollection, value); }
        }

        private bool isOnline;
        public bool IsOnline
        {
            get { return isOnline; }
            private set { SetProperty(ref isOnline, value); }
        }

        #endregion

        #region ViewModels

        //Settings ViewModel
        private SettingsViewModel _settingsVM;
        public SettingsViewModel SettingsVM
        {
            get { return _settingsVM; }
            set { SetProperty(ref _settingsVM, value); }
        }

        private CollectionViewModel _collectionVM;
        public CollectionViewModel CollectionVM
        {
            get { return _collectionVM; }
            private set { SetProperty(ref _collectionVM, value); }
        }

        private LayoutViewModel _currentLayout;
        public LayoutViewModel CurrentLayout
        {
            get { return _currentLayout; }
            set { SetProperty(ref _currentLayout, value); }
        }

        //Tag Creation ViewModel
        private IEditViewModel _addTagViewModel;
        public IEditViewModel AddTagViewModel
        {
            get { return _addTagViewModel; }
            set { SetProperty(ref _addTagViewModel, value); }
        }

        //Tags and Filters Tabs ViewModel (Left Dock)
        private TagsFiltersViewModel _tfViewModel;
        public TagsFiltersViewModel TFViewModel
        {
            get { return _tfViewModel; }
            set { SetProperty(ref _tfViewModel, value); }
        }

        //Import ViewModel
        private ImportViewModel _currentImportVM;
        public ImportViewModel CurrentImportViewModel
        {
            get { return _currentImportVM; }
            set { SetProperty(ref _currentImportVM, value); }
        }

        #endregion

        #region Commands and functions for MainWindow

        public RelayCommand<string> OpenSettingsCommand { get; private set; }

        public void OpenSettings(string tab = null)
        {
            var settingswindow = new SettingsWindow(SettingsVM,tab);
            settingswindow.Show();
        }

        //Change Fileview
        public RelayCommand<CodexLayout> ChangeFileViewCommand { get; private set; }
        public void ChangeFileView(CodexLayout v)
        {
            Properties.Settings.Default.PreferedView = (int)v;
            CurrentLayout = v switch
            {
                CodexLayout.HomeLayout => new HomeLayoutViewModel(),
                CodexLayout.ListLayout => new ListLayoutViewModel(),
                CodexLayout.CardLayout => new CardLayoutViewModel(),
                CodexLayout.TileLayout => new TileLayoutViewModel(),
                _ => null
            };
        }

        //Reset
        public ActionCommand ResetCommand { get; private set; }

        public void Refresh()
        {
            CollectionVM.ReFilter();
            TFViewModel.TagsTabVM.RefreshTreeView();
        }

        public void Reset()
        {
            CollectionVM.ClearFilters();
            TFViewModel.TagsTabVM.RefreshTreeView();
        }

        //Add Tag Btn
        public ActionCommand AddTagCommand { get; private set; }
        public void AddTag()
        {
            AddTagViewModel = new TagEditViewModel(null);
        }

        //Import Btn
        public RelayCommand<Sources> ImportFilesCommand { get; private set; }
        public void ImportFiles(Sources source)
        {
            CurrentImportViewModel = new ImportViewModel(source, CurrentCollection);
        } 

        //Change Collection
        public void ChangeCollection(string collectionDir)
        {
            CurrentCollection = new CodexCollection(collectionDir);            
            CollectionVM = new CollectionViewModel(_currentCollection);
            ChangeFileView((CodexLayout)Properties.Settings.Default.PreferedView);
            TFViewModel = new TagsFiltersViewModel();
            AddTagViewModel = new TagEditViewModel(null);
        }

        //Add new CodexCollection
        public RelayCommand<string> CreateCollectionCommand { get; private set; }
        public void CreateCollection(string dirName)
        {
            if (string.IsNullOrEmpty(dirName)) return;

            Directory.CreateDirectory((CodexCollection.CollectionsPath + dirName + @"\CoverArt"));
            Directory.CreateDirectory((CodexCollection.CollectionsPath + dirName + @"\Thumbnails"));
            CollectionDirectories.Add(dirName);
            CurrentCollectionName = dirName;
        }

        //Rename Collection
        public RelayCommand<string> EditCollectionNameCommand { get; private set; }
        public void EditCollectionName(string newName)
        {
            var index = CollectionDirectories.IndexOf(CurrentCollectionName);
            CurrentCollection.RenameCollection(newName);
            CollectionDirectories[index] = newName;
            CurrentCollectionName = newName;
        }

        //Delete Collection
        public ActionCommand DeleteCollectionCommand { get; private set; }
        public void RaiseDeleteCollectionWarning()
        {
            if (CurrentCollection.AllCodices.Count > 0)
            {
                //MessageBox "Are you Sure?"
                string sCaption = "Are you Sure?";

                string MessageSingle = "There is still one file in this collection, if you don't want to remove these from COMPASS, move them to another collection first. Are you sure you want to continue?";
                string MessageMultiple = "There are still" + _currentCollection.AllCodices.Count + " files in this collection, if you don't want to remove these from COMPASS, move them to another collection first. Are you sure you want to continue?";

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
            Directory.Delete(CodexCollection.CollectionsPath + todelete,true);
        }

        //Search
        private RelayCommand<string> _searchCommand;
        public RelayCommand<string> SearchCommand => _searchCommand ??= new(CollectionVM.UpdateSearchFilteredFiles);

        //called every few seconds to update IsOnline
        private void CheckConnection(object sender, EventArgs e)
        {
            IsOnline = Utils.PingURL();
        }

        public ActionCommand CheckForUpdatesCommand { get; init; }
        private void CheckForUpdates()
        {
            AutoUpdater.Start("https://raw.githubusercontent.com/DSPAUL/COMPASS/master/versionInfo.xml");
        }
        #endregion
    }
}
