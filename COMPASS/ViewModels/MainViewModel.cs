using COMPASS.Tools;
using COMPASS.ViewModels.Commands;
using ImageMagick;
using System;
using static COMPASS.Tools.Enums;

namespace COMPASS.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public MainViewModel(string FolderName)
        {
            currentData = new Data(FolderName);
            filterHandler = new FilterHandler(currentData);
            CurrentFileViewModel = new FileListViewModel(this);
            TFViewModel = new TagsFiltersViewModel(this);

            MagickNET.SetGhostscriptDirectory(@"C:\Users\pauld\Documents\COMPASS\COMPASS\Libraries");

            //Commands
            ChangeFileViewCommand = new SimpleCommand(ChangeFileView);
            ResetCommand = new BasicCommand(Reset);
            AddTagCommand = new BasicCommand(AddTag);
            ImportFilesCommand = new SimpleCommand(ImportFiles);
        }

        #region Properties

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
        #endregion
    }
}
