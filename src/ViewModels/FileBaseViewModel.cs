using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.ViewModels.Commands;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace COMPASS.ViewModels
{
    public class FileBaseViewModel : ObservableObject
    {
        public FileBaseViewModel(MainViewModel vm)
        {
            mainViewModel = vm;
            
            //commands
            EditFileCommand = new BasicCommand(EditFile);
            EditFilesCommand = new RelayCommand<object>(EditFiles);
            OpenFileLocallyCommand = new RelayCommand<object>(OpenFileLocally, CanOpenFileLocally);
            OpenFileOnlineCommand = new RelayCommand<object>(OpenFileOnline,CanOpenFileOnline);
            MoveToFolderCommand = new RelayCommand<object>(MoveToFolder);
            DeleteFileCommand = new RelayCommand<object>(DeleteFile);
            OpenSelectedFilesCommand = new RelayCommand<object>(OpenSelectedFiles);

            ViewOptions = new ObservableCollection<MyMenuItem>();
            getSortOptions();
            SortOptionsMenuItem = new MyMenuItem("Sorty By")
            {
                Submenus = SortOptions
            };

            OpenFilePriority = new List<Func<object, bool>>() { OpenFileLocally, OpenFileOnline };
        }

        #region Properties

        //MainViewModel
        private MainViewModel mainViewModel;
        public MainViewModel MVM
        {
            get { return mainViewModel; }
            set { SetProperty(ref mainViewModel, value); }
        }

        //Selected File
        private Codex selectedFile;
        public Codex SelectedFile {
            get { return selectedFile; } 
            set { SetProperty(ref selectedFile, value); }
        }

        //list with options to costimize view
        private ObservableCollection<MyMenuItem> viewOptions;
        public ObservableCollection<MyMenuItem> ViewOptions
        {
            get { return viewOptions; }
            set { SetProperty(ref viewOptions, value); }
        }

        //list with options to sort the files
        private MyMenuItem sortOptionsMenuItem;
        public MyMenuItem SortOptionsMenuItem
        {
            get { return sortOptionsMenuItem; }
            set { SetProperty(ref sortOptionsMenuItem, value); }
        }

        //list with options to sort the files
        private ObservableCollection<MyMenuItem> sortOptions;
        public ObservableCollection<MyMenuItem> SortOptions
        {
            get { return sortOptions; }
            set { SetProperty(ref sortOptions, value); }
        }

        //list with functions in prefered execution order to try and open a file
        private List<Func<object, bool>> openFilePriority;
        public List<Func<object,bool>> OpenFilePriority
        {
            get { return openFilePriority; }
            set { SetProperty(ref openFilePriority, value); }
        }
        #endregion

        #region Functions and Commands

        private void getSortOptions()
        {
            SortOptions = new ObservableCollection<MyMenuItem>();
            
            var SortPropertyNames = new List<string>()
            {
                "Title",
                "Author",
                "Publisher",
                "ReleaseDate",
                "Rating",
                "PageCount"
            };

            //double check on typos by checking if all property names exist in codex class
            var PossibleSortProptertyNames = typeof(Codex).GetProperties().Select(p => p.Name).ToList();
            if (SortPropertyNames.Except(PossibleSortProptertyNames).Any())
            {
                MessageBox.Show("One of the sort property paths does not exist");
            }

            foreach(var sortOption in SortPropertyNames)
            {
                sortOptions.Add(new MyMenuItem(sortOption) 
                { 
                    Command = new RelayCommand<object>(sortByCommandHelper),
                    CommandParam = sortOption
                });
            }
        }

        private bool sortByCommandHelper(object o)
        {
            MVM.FilterHandler.SortBy((string)o);
            return true;
        }

        //Open File whereever
        public bool OpenFile(Codex codex = null)
        {
            Codex ToOpen = codex != null ? codex : MVM.CurrentFileViewModel.SelectedFile;

            //attempt to open file
            bool success = false;
            int i = 0;
            while (!success)
            {
                success = OpenFilePriority[i](codex);
                i++;
                //break if all options tried
                if (i >= openFilePriority.Count)
                {
                    break;
                }
            }
            if(success) { return true; }
            else
            {
                MessageBox.Show("Could not open file, please check local path or URL");
                return false;
            }
        }

        //Open File Offline
        public RelayCommand<object> OpenFileLocallyCommand { get; private set; }
        public bool OpenFileLocally(object o = null)
        {
            Codex ToOpen = o != null ? (Codex)o : MVM.CurrentFileViewModel.SelectedFile;
            try
            {
                Process.Start(new ProcessStartInfo(ToOpen.Path) {UseShellExecute = true });
                return true;
            }
            catch
            {
                return false;
            }
        }
        public bool CanOpenFileLocally(object o = null)
        {
            Codex ToOpen = o != null ? (Codex)o : MVM.CurrentFileViewModel.SelectedFile;

            //if SelectedFile is also null
            if (ToOpen == null) return false;

            return ToOpen.HasOfflineSource();
        }

        //Open File Online
        public RelayCommand<object> OpenFileOnlineCommand { get; private set; }
        public bool OpenFileOnline(object o = null)
        {
            Codex ToOpen = o != null ? (Codex)o : MVM.CurrentFileViewModel.SelectedFile;

            try
            {
                Process.Start(new ProcessStartInfo(ToOpen.SourceURL) { UseShellExecute = true });
                return true;
            }
            catch
            {
                return false;
            }
            
        }
        public bool CanOpenFileOnline(object o = null)
        {
            Codex ToOpen = o != null ? (Codex)o : MVM.CurrentFileViewModel.SelectedFile;

            //if SelectedFile is also null
            if (ToOpen == null) return false;

            return ToOpen.HasOnlineSource();
        }

        //Open Multiple Files
        public RelayCommand<object> OpenSelectedFilesCommand { get; private set; }
        public bool OpenSelectedFiles(object o = null)
        {
            if (o == null) return false;
            IList list = o as IList;
            List<Codex> ToOpen = list.Cast<Codex>().ToList();
            //MessageBox "Are you Sure?"
            string sMessageBoxText = "You are about to open " + ToOpen.Count + " Files. Are you sure you wish to continue?";
            string sCaption = "Are you Sure?";

            MessageBoxButton btnMessageBox = MessageBoxButton.YesNo;
            MessageBoxImage imgMessageBox = MessageBoxImage.Warning;

            MessageBoxResult rsltMessageBox = MessageBox.Show(sMessageBoxText, sCaption, btnMessageBox, imgMessageBox);

            if (rsltMessageBox == MessageBoxResult.Yes)
            {
                foreach(Codex f in ToOpen)
                {
                    try
                    {
                        if(f.Path != null) Process.Start(new ProcessStartInfo(f.Path) { UseShellExecute = true });
                        else Process.Start(new ProcessStartInfo(f.SourceURL) { UseShellExecute = true });
                    }
                    catch
                    {
                        MessageBox.Show("File Path Invalid");
                    }
                }
                return true;
            }
            else { return false; }
        }

        //Edit File
        public BasicCommand EditFileCommand { get; private set; }
        public void EditFile()
        {
            MVM.CurrentEditViewModel = new FileEditViewModel(MVM, MVM.CurrentFileViewModel.SelectedFile);
            FilePropWindow fpw = new FilePropWindow((FileEditViewModel)MVM.CurrentEditViewModel);
            fpw.ShowDialog();
            fpw.Topmost = true;
        }

        //Edit Multiple files
        public RelayCommand<object> EditFilesCommand { get; private set; }
        public bool EditFiles(object o = null)
        {
            if (o == null) return false;
            IList list = o as IList;
            List<Codex> ToEdit = list.Cast<Codex>().ToList();
            MVM.CurrentEditViewModel = new FileBulkEditViewModel(MVM, ToEdit);
            FileBulkEditWindow fpw = new FileBulkEditWindow((FileBulkEditViewModel)MVM.CurrentEditViewModel);
            fpw.ShowDialog();
            fpw.Topmost = true;
            return true;
        }

        //Move Codex to other CodexCollection
        public RelayCommand<object> MoveToFolderCommand { get; private set; }
        public bool MoveToFolder(object o = null)
        {
            //par contains 2 parameters
            var par = (object[])o;
            List<Codex> ToMoveList = new List<Codex>();
            string targetCollectionName;

            //extract Collection parameter
            if (par[0] != null) targetCollectionName = (string)(par[0]);
            else return false;
            if (targetCollectionName == MVM.CurrentFolder) return false;

            //extract Codex parameter
            if (par[1] != null)
            {
                IList list = par[1] as IList;
                ToMoveList = list.Cast<Codex>().ToList();
            }
            else ToMoveList.Add(MVM.CurrentFileViewModel.selectedFile);

            //MessageBox "Are you Sure?"
            string MessageSingle = "Moving " + ToMoveList[0].Title + " to " + targetCollectionName + " will remove all tags from the Codex, are you sure you wish to continue?";
            string MessageMultiple = "Moving these " + ToMoveList.Count() + " files to " + targetCollectionName + " will remove all tags from the Codices, are you sure you wish to continue?";

            string sCaption = "Are you Sure?";
            string sMessageBoxText = ToMoveList.Count == 1 ? MessageSingle : MessageMultiple;

            MessageBoxButton btnMessageBox = MessageBoxButton.YesNo;
            MessageBoxImage imgMessageBox = MessageBoxImage.Warning;

            MessageBoxResult rsltMessageBox = MessageBox.Show(sMessageBoxText, sCaption, btnMessageBox, imgMessageBox);

            if (rsltMessageBox == MessageBoxResult.Yes) 
            {
                CodexCollection TargetCollection = new CodexCollection(targetCollectionName);
                foreach (Codex ToMove in ToMoveList)
                {
                    ToMove.Tags.Clear();
                    // Give file new ID and move it to other folder
                    ToMove.ID = Utils.GetAvailableID(TargetCollection.AllFiles);

                    //Add Codex to target CodexCollection
                    TargetCollection.AllFiles.Add(ToMove);

                    //Update Author and Publisher List
                    if (ToMove.Author != "" && !TargetCollection.AuthorList.Contains(ToMove.Author)) TargetCollection.AuthorList.Add(ToMove.Author);
                    if (ToMove.Publisher != "" && !TargetCollection.PublisherList.Contains(ToMove.Publisher)) TargetCollection.PublisherList.Add(ToMove.Publisher);

                    //Move cover art to right folder with new ID
                    string newCoverArt = CodexCollection.CollectionsPath + targetCollectionName + @"\CoverArt\" + ToMove.ID + ".png";
                    string newThumbnail = CodexCollection.CollectionsPath + targetCollectionName + @"\Thumbnails\" + ToMove.ID + ".png";
                    File.Copy(ToMove.CoverArt, newCoverArt);
                    File.Copy(ToMove.Thumbnail, newThumbnail);

                    //Delete file in original folder
                    MVM.CurrentCollection.DeleteFile(ToMove);
                    MVM.FilterHandler.RemoveFile(ToMove);

                    //Update the cover art metadata to new path, has to happen after delete so old one gets deleted
                    ToMove.CoverArt = newCoverArt;
                    ToMove.Thumbnail = newThumbnail;
                }
                //Save changes to TargetCollection
                TargetCollection.SaveFilesToFile();
            }
            return true;
        }

        //Delete File
        public RelayCommand<object> DeleteFileCommand { get; private set; }
        public bool DeleteFile(object o = null)
        {
            List<Codex> ToDeleteList = new List<Codex>();
            if (o == null) ToDeleteList.Add(MVM.CurrentFileViewModel.SelectedFile);
            else
            {
                IList list = o as IList;
                ToDeleteList = list.Cast<Codex>().ToList();
            }
            foreach(Codex ToDelete in ToDeleteList)
            {
                MVM.CurrentCollection.DeleteFile(ToDelete);
                MVM.FilterHandler.RemoveFile(ToDelete);
            }
            return true;
        }
        #endregion
    }
}
