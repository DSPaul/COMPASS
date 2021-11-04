using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.ViewModels.Commands;
using ImageMagick;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
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
            //Get all RPG systems by folder name
            Folders = new ObservableCollection<string>();
            string [] FullPathFolders = Directory.GetDirectories(CodexCollection.CollectionsPath);
            foreach(string p in FullPathFolders){
                Folders.Add(Path.GetFileName(p));
            }

            //in case of first boot, create default folder
            if (Folders.Count == 0) 
            {
                CreateFolder("Default");
            }

            //in case startup collection no longer exists
            else if(!Directory.Exists(CodexCollection.CollectionsPath + Properties.Settings.Default.StartupCollection))
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

            //Get webdriver for Selenium
            InitWebdriver();

            //MagickNET.SetGhostscriptDirectory(AppDomain.CurrentDomain.BaseDirectory);

            //CheckForUpdates();

            //Start internet checkup timer
            DispatcherTimer CheckConnectionTimer = new DispatcherTimer();
            CheckConnectionTimer.Tick += new EventHandler(CheckConnection);
            CheckConnectionTimer.Interval = new TimeSpan(0, 0, 30);
            CheckConnectionTimer.Start();

            //Commands
            ChangeFileViewCommand = new SimpleCommand(ChangeFileView);
            ResetCommand = new BasicCommand(Reset);
            AddTagCommand = new BasicCommand(AddTag);
            ImportFilesCommand = new SimpleCommand(ImportFiles);
            CreateFolderCommand = new SimpleCommand(CreateFolder);
            EditFolderCommand = new SimpleCommand(EditFolder);
            DeleteFolderCommand = new BasicCommand(RaiseDeleteFolderWarning);
            SearchCommand = new SimpleCommand(Search);
        }

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
            get { return pingURL(); }
            private set { SetProperty(ref isOnline, value); }
        }

        #endregion

        #region Handlers and ViewModels

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

        #region Functions and Commands

        //Change Fileview
        public SimpleCommand ChangeFileViewCommand { get; private set; }
        public void ChangeFileView(Object v)
        {
            v = (FileView)v;
            Properties.Settings.Default.PreferedView = (int)v;
            switch (v)
            {
                case FileView.ListView:
                    CurrentFileViewModel = new FileListViewModel(this);
                    break;
                case FileView.CardView:
                    CurrentFileViewModel = new FileCardViewModel(this);
                    break;
                case FileView.TileView:
                    CurrentFileViewModel = new FileTileViewModel(this);
                    break;
            }
        }

        //Reset
        public BasicCommand ResetCommand { get; private set; }
        public void Reset()
        {
            FilterHandler.ClearFilters();
            TFViewModel.RefreshTreeView();
        }

        //Add Tag Btn
        public BasicCommand AddTagCommand { get; private set; }
        public void AddTag()
        {
            AddTagViewModel = new TagEditViewModel(this, null);
        }

        //Import Btn
        public SimpleCommand ImportFilesCommand { get; private set; }
        public void ImportFiles(object mode)
        {
            CurrentImportViewModel = new ImportViewModel(this, (ImportMode)mode);
        } 

        //Change Folder
        public void ChangeFolder(string folder)
        {
            CurrentCollection = new CodexCollection(folder);
            FilterHandler = new FilterHandler(currentCollection);
            ChangeFileView(Properties.Settings.Default.PreferedView);
            TFViewModel = new TagsFiltersViewModel(this);
            AddTagViewModel = new TagEditViewModel(this, null);
        }

        //Add new Folder / CodexCollection
        public SimpleCommand CreateFolderCommand { get; private set; }
        public void CreateFolder(object folder)
        {
            string f = (string)folder;
            Directory.CreateDirectory((CodexCollection.CollectionsPath + f + @"\CoverArt"));
            Folders.Add(f);
            CurrentFolder = f;
        }

        //Rename Folder/Collection/RPG System
        public SimpleCommand EditFolderCommand { get; private set; }
        public void EditFolder(object folder)
        {
            string f = (string)folder;
            var index = Folders.IndexOf(CurrentFolder);
            CurrentCollection.RenameFolder(f);
            Folders[index] = f;
            CurrentFolder = f;
        }

        //Delete Folder/Collection/RPG System
        public BasicCommand DeleteFolderCommand { get; private set; }
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
        public SimpleCommand SearchCommand { get; private set; }
        public void Search(object o)
        {
            string searchterm = (string)o;
            FilterHandler.UpdateSearchFilteredFiles(searchterm);
        }

        //check internet connection
        public bool pingURL(string URL = "8.8.8.8")
        { 
            Ping p = new Ping();
            try
            {
                PingReply reply = p.Send(URL, 3000);
                if (reply.Status == IPStatus.Success)
                    return true;
            }
            catch { }
            return false;
        }

        //used to Update IsOnline
        private void CheckConnection(object sender, EventArgs e)
        {
            IsOnline = pingURL();
        }

        private void InitWebdriver()
        {
            if (IsInstalled("chrome.exe"))
            {
                Properties.Settings.Default.SeleniumBrowser = (int)Enums.Browser.Chrome;
                new DriverManager().SetUpDriver(new ChromeConfig(), WebDriverManager.Helpers.VersionResolveStrategy.MatchingBrowser);
            }
            else if (IsInstalled("firefox.exe"))
            {
                Properties.Settings.Default.SeleniumBrowser = (int)Enums.Browser.Firefox;
                new DriverManager().SetUpDriver(new FirefoxConfig());
            }

            else
            {
                Properties.Settings.Default.SeleniumBrowser = (int)Enums.Browser.Edge;
                new DriverManager().SetUpDriver(new EdgeConfig());
            }
        }

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
    }
}
