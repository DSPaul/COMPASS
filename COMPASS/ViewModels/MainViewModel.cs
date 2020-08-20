using COMPASS.Tools;
using COMPASS.ViewModels.Commands;
using ImageMagick;
using Squirrel;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
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
            //Commands
            ChangeFileViewCommand = new SimpleCommand(ChangeFileView);
            ResetCommand = new BasicCommand(Reset);
            AddTagCommand = new BasicCommand(AddTag);
            ImportFilesCommand = new SimpleCommand(ImportFiles);
            CreateFolderCommand = new SimpleCommand(CreateFolder);
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
                
                ChangeFolder(value);
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
        #endregion

        //Add new Folder/collection/RPG System
        public SimpleCommand CreateFolderCommand { get; private set; }
        public void CreateFolder(object folder)
        {
            string f = (string)folder;
            Directory.CreateDirectory((Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Compass\Collections\" + f + @"\CoverArt"));
            _Folders.Add(f);
            CurrentFolder = f;
        }
        private async Task CheckForUpdates()
        {
            using (var mgr = UpdateManager.GitHubUpdateManager("https://github.com/DSPAUL/COMPASS"))
            {
              await mgr.Result.UpdateApp();
            }
        }
    }
}
