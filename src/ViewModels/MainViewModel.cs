using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.ViewModels.Commands;
using COMPASS.Windows;
using ImageMagick;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using static COMPASS.Tools.Enums;

namespace COMPASS.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        public MainViewModel()
        {
            BaseViewModel.MVM = this;
            SettingsVM = new SettingsViewModel();
            
            //Load data
            InitCollection();

            //Get webdriver for Selenium
            InitWebdriver();

            MagickNET.SetGhostscriptDirectory(AppDomain.CurrentDomain.BaseDirectory + @"\gs");

            //CheckForUpdates();

            //Start internet checkup timer
            DispatcherTimer CheckConnectionTimer = new DispatcherTimer();
            CheckConnectionTimer.Tick += new EventHandler(CheckConnection);
            CheckConnectionTimer.Interval = new TimeSpan(0, 0, 30);
            CheckConnectionTimer.Start();
            //to check right away on startup
            IsOnline = Utils.pingURL();

            //Commands
            ChangeFileViewCommand = new RelayCommand<FileView>(ChangeFileView);
            ResetCommand = new ActionCommand(Reset);
            AddTagCommand = new ActionCommand(AddTag);
            ImportFilesCommand = new RelayCommand<Sources>(ImportFiles);
            CreateFolderCommand = new RelayCommand<string>(CreateFolder);
            EditFolderCommand = new RelayCommand<string>(EditFolder);
            DeleteFolderCommand = new ActionCommand(RaiseDeleteFolderWarning);
            SearchCommand = new RelayCommand<string>(Search);
            OpenSettingsCommand = new RelayCommand<string>(OpenSettings);
        }

        #region Init Functions

        //Get a collection at startup
        private void InitCollection()
        {
            //Get all RPG systems by folder name
            Folders = new ObservableCollection<string>();
            string[] FullPathFolders = Directory.GetDirectories(CodexCollection.CollectionsPath);
            foreach (string p in FullPathFolders)
            {
                Folders.Add(Path.GetFileName(p));
            }

            //in case of first boot, create default folder
            if (Folders.Count == 0)
            {
                CreateFolder("Default");
            }

            //in case startup collection no longer exists
            else if (!Directory.Exists(CodexCollection.CollectionsPath + Properties.Settings.Default.StartupCollection))
            {
                MessageBox.Show("The collection " + Properties.Settings.Default.StartupCollection + " could not be found. ");
                //pick first one that does exists
                foreach (string f in Folders)
                {
                    if (Directory.Exists(CodexCollection.CollectionsPath + f))
                    {
                        CurrentFolder = f;
                        break;
                    }
                }
            }

            //otherwise, open startup collection
            else
            {
                CurrentFolder = Properties.Settings.Default.StartupCollection;
            }
        }

        //Get latest version of relevant Webdriver for selenium
        private void InitWebdriver()
        {
            if (IsInstalled("chrome.exe"))
            {
                Properties.Settings.Default.SeleniumBrowser = (int)Enums.Browser.Chrome;
                try
                {
                    new DriverManager().SetUpDriver(new ChromeConfig(), WebDriverManager.Helpers.VersionResolveStrategy.MatchingBrowser);
                }
                catch { }
            }
            else if (IsInstalled("firefox.exe"))
            {
                Properties.Settings.Default.SeleniumBrowser = (int)Enums.Browser.Firefox;
                try
                {
                    new DriverManager().SetUpDriver(new FirefoxConfig());
                }
                catch { }
            }

            else
            {
                Properties.Settings.Default.SeleniumBrowser = (int)Enums.Browser.Edge;
                try
                {
                    new DriverManager().SetUpDriver(new EdgeConfig());
                }
                catch { }
            }
        }

        //helper function for InitWebdriver to check if certain browsers are installed
        private bool IsInstalled(string name)
        {
            string currentUserRegistryPathPattern = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\App Paths\";
            string localMachineRegistryPathPattern = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\";

            var currentUserPath = Microsoft.Win32.Registry.GetValue(currentUserRegistryPathPattern + name, "", null);
            var localMachinePath = Microsoft.Win32.Registry.GetValue(localMachineRegistryPathPattern + name, "", null);

            if (currentUserPath != null | localMachinePath != null)
            {
                return true;
            }
            return false;
        }
        #endregion

        #region Properties
        private ObservableCollection<string> _Folders;
        public ObservableCollection<string> Folders
        {
            get { return _Folders; }
            set { SetProperty(ref _Folders, value); }
        }

        private string currentFolder;
        public string CurrentFolder
        {
            get { return currentFolder; }
            set
            {
                if (CurrentCollection != null)
                {
                    CurrentCollection.SaveFilesToFile();
                    CurrentCollection.SaveTagsToFile();
                }
                if(value != null) ChangeFolder(value);
                SetProperty(ref currentFolder, value);
            }
        }

        //CodexCollection 
        private CodexCollection currentCollection;
        public CodexCollection CurrentCollection
        {
            get { return currentCollection; }
            private set { SetProperty(ref currentCollection, value); }
        }

        private bool isOnline;
        public bool IsOnline
        {
            get { return isOnline; }
            private set { SetProperty(ref isOnline, value); }
        }

        #endregion

        #region Handlers and ViewModels

        //Settings ViewModel
        private SettingsViewModel _SettingsVM;
        public SettingsViewModel SettingsVM
        {
            get { return _SettingsVM; }
            set { SetProperty(ref _SettingsVM, value); }
        }

        //Filter Handler
        private FilterHandler filterHandler;
        public FilterHandler FilterHandler
        {
            get { return filterHandler; }
            private set { SetProperty(ref filterHandler, value); }
        }

        //File ViewModel
        private FileBaseViewModel currentFileViewModel;
        public FileBaseViewModel CurrentFileViewModel
        {
            get { return currentFileViewModel; }
            set { SetProperty(ref currentFileViewModel, value); }
        }

        //Edit ViewModel
        private BaseEditViewModel currentEditViewModel;
        public BaseEditViewModel CurrentEditViewModel
        {
            get { return currentEditViewModel; }
            set { SetProperty(ref currentEditViewModel, value); }
        }

        //Tag Creation ViewModel
        private BaseEditViewModel addTagViewModel;
        public BaseEditViewModel AddTagViewModel
        {
            get { return addTagViewModel; }
            set { SetProperty(ref addTagViewModel, value); }
        }

        //Tags and Filters Tabs ViewModel (Left Dock)
        private TagsFiltersViewModel tfViewModel;
        public TagsFiltersViewModel TFViewModel
        {
            get { return tfViewModel; }
            set { SetProperty(ref tfViewModel, value); }
        }

        //Import ViewModel
        private ImportViewModel currentimportViewModel;
        public ImportViewModel CurrentImportViewModel
        {
            get { return currentimportViewModel; }
            set { SetProperty(ref currentimportViewModel, value); }
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
        public RelayCommand<FileView> ChangeFileViewCommand { get; private set; }
        public void ChangeFileView(FileView v)
        {
            Properties.Settings.Default.PreferedView = (int)v;
            switch (v)
            {
                case FileView.ListView:
                    CurrentFileViewModel = new FileListViewModel();
                    break;
                case FileView.CardView:
                    CurrentFileViewModel = new FileCardViewModel();
                    break;
                case FileView.TileView:
                    CurrentFileViewModel = new FileTileViewModel();
                    break;
            }
        }

        //Reset
        public ActionCommand ResetCommand { get; private set; }

        public void Refresh()
        {
            FilterHandler.ReFilter();
            TFViewModel.RefreshTreeView();
        }

        public void Reset()
        {
            FilterHandler.ClearFilters();
            TFViewModel.RefreshTreeView();
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
            CurrentImportViewModel = new ImportViewModel(source);
        } 

        //Change Collection
        public void ChangeFolder(string folder)
        {
            CurrentCollection = new CodexCollection(folder);            
            FilterHandler = new FilterHandler(currentCollection);
            ChangeFileView((FileView)Properties.Settings.Default.PreferedView);
            TFViewModel = new TagsFiltersViewModel();
            AddTagViewModel = new TagEditViewModel(null);
        }

        //Add new Folder / CodexCollection
        public RelayCommand<string> CreateFolderCommand { get; private set; }
        public void CreateFolder(string folder)
        {
            Directory.CreateDirectory((CodexCollection.CollectionsPath + folder + @"\CoverArt"));
            Directory.CreateDirectory((CodexCollection.CollectionsPath + folder + @"\Thumbnails"));
            Folders.Add(folder);
            CurrentFolder = folder;
        }

        //Rename Folder/Collection/RPG System
        public RelayCommand<string> EditFolderCommand { get; private set; }
        public void EditFolder(string folder)
        {
            var index = Folders.IndexOf(CurrentFolder);
            CurrentCollection.RenameFolder(folder);
            Folders[index] = folder;
            CurrentFolder = folder;
        }

        //Delete Folder/Collection/RPG System
        public ActionCommand DeleteFolderCommand { get; private set; }
        public void RaiseDeleteFolderWarning()
        {
            if (CurrentCollection.AllFiles.Count > 0)
            {
                //MessageBox "Are you Sure?"
                string sCaption = "Are you Sure?";

                string MessageSingle = "There is still one file in this collection, if you don't want to remove these from COMPASS, move them to another collection first. Are you sure you want to continue?";
                string MessageMultiple = "There are still" + currentCollection.AllFiles.Count + " files in this collection, if you don't want to remove these from COMPASS, move them to another collection first. Are you sure you want to continue?";

                string sMessageBoxText = CurrentCollection.AllFiles.Count == 1 ? MessageSingle : MessageMultiple;

                MessageBoxButton btnMessageBox = MessageBoxButton.YesNo;
                MessageBoxImage imgMessageBox = MessageBoxImage.Warning;

                MessageBoxResult rsltMessageBox = MessageBox.Show(sMessageBoxText, sCaption, btnMessageBox, imgMessageBox);

                if (rsltMessageBox == MessageBoxResult.Yes)
                {
                    DeleteFolder(CurrentFolder);
                }
            }
            else
            {
                DeleteFolder(CurrentFolder);
            }
        }
        public void DeleteFolder(string todelete)
        {
            Folders.Remove(todelete);
            CurrentFolder = Folders[0];
            Directory.Delete(CodexCollection.CollectionsPath + todelete,true);
        }

        //Search
        public RelayCommand<string> SearchCommand { get; private set; }
        public void Search(string searchterm)
        {
            FilterHandler.UpdateSearchFilteredFiles(searchterm);
        }

        //called every few seconds to update IsOnline
        private void CheckConnection(object sender, EventArgs e)
        {
            IsOnline = Utils.pingURL();
        }

        #endregion
    }
}
