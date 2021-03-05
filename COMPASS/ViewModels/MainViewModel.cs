using COMPASS.Tools;
using COMPASS.ViewModels.Commands;
using ImageMagick;
using Squirrel;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using static COMPASS.Tools.Enums;

namespace COMPASS.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public MainViewModel(string FolderName)
        {
            //Get all RPG systems by folder name
            Folders = new ObservableCollection<string>();
            string [] FullPathFolders = Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Compass\Collections\");
            foreach(string p in FullPathFolders){
                Folders.Add(Path.GetFileName(p));
            }

            CurrentFolder = FolderName;

            MagickNET.SetGhostscriptDirectory(AppDomain.CurrentDomain.BaseDirectory);

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
                if (CurrentData != null)
                {
                    CurrentData.SaveFilesToFile();
                    CurrentData.SaveTagsToFile();
                }
                if(value != null) ChangeFolder(value);
                SetProperty(ref currentFolder, value);
            }
        }

        //Data 
        private Data currentData;
        public Data CurrentData
        {
            get { return currentData; }
            private set { SetProperty(ref currentData, value); }
        }

        private bool isOnline;
        public bool IsOnline
        {
            get { return IsConnectedToInternet(); }
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
            switch (v)
            {
                case FileView.ListView:
                    CurrentFileViewModel = new FileListViewModel(this);
                    break;
                case FileView.MixView:
                    CurrentFileViewModel = new FileMixViewModel(this);
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
            CurrentData = new Data(folder);
            FilterHandler = new FilterHandler(currentData);
            CurrentFileViewModel = new FileListViewModel(this);
            TFViewModel = new TagsFiltersViewModel(this);
            AddTagViewModel = new TagEditViewModel(this, null);
        }

        //Add new Folder/collection/RPG System
        public SimpleCommand CreateFolderCommand { get; private set; }
        public void CreateFolder(object folder)
        {
            string f = (string)folder;
            Directory.CreateDirectory((Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Compass\Collections\" + f + @"\CoverArt"));
            _Folders.Add(f);
            CurrentFolder = f;
        }

        //Rename Folder/Collection/RPG System
        public SimpleCommand EditFolderCommand { get; private set; }
        public void EditFolder(object folder)
        {
            string f = (string)folder;
            var index = Folders.IndexOf(CurrentFolder);
            CurrentData.RenameFolder(f);
            Folders[index] = f;
            CurrentFolder = f;
        }

        //Delete Folder/Collection/RPG System
        public BasicCommand DeleteFolderCommand { get; private set; }
        public void RaiseDeleteFolderWarning()
        {
            if (CurrentData.AllFiles.Count > 0)
            {
                //MessageBox "Are you Sure?"
                string sCaption = "Are you Sure?";

                string MessageSingle = "There is still one file in this collection, if you don't want to remove these from COMPASS, move them to another collection first. Are you sure you want to continue?";
                string MessageMultiple = "There are still" + currentData.AllFiles.Count + " files in this collection, if you don't want to remove these from COMPASS, move them to another collection first. Are you sure you want to continue?";

                string sMessageBoxText = CurrentData.AllFiles.Count == 1 ? MessageSingle : MessageMultiple;

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
            Directory.Delete(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Compass\Collections\" + todelete,true);
        }

        //check internet connection
        public bool IsConnectedToInternet()
        { 
            Ping p = new Ping();
            try
            {
                PingReply reply = p.Send("8.8.8.8", 3000);
                if (reply.Status == IPStatus.Success)
                    return true;
            }
            catch { }
            return false;
        }
        private void CheckConnection(object sender, EventArgs e)
        {
            IsOnline = IsConnectedToInternet();
        }
        #endregion


        private async Task CheckForUpdates()
        {
            using (var mgr = UpdateManager.GitHubUpdateManager("https://github.com/DSPAUL/COMPASS"))
            {
              await mgr.Result.UpdateApp();
            }
        }



    }
}
