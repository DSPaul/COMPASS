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
    public abstract class FileBaseViewModel : BaseViewModel
    {
        public FileBaseViewModel()
        {            
            //commands
            EditFileCommand = new(EditFile);
            EditFilesCommand = new(EditFiles);
            OpenFileLocallyCommand = new(OpenFileLocally, CanOpenFileLocally);
            OpenFileOnlineCommand = new(OpenFileOnline,CanOpenFileOnline);
            MoveToFolderCommand = new(MoveToFolder);
            DeleteFileCommand = new(DeleteFile);
            OpenSelectedFilesCommand = new(OpenSelectedFiles);
            ShowInExplorerCommand = new(ShowInExplorer, CanOpenFileLocally);

            ViewOptions = new ObservableCollection<MyMenuItem>();
            getSortOptions();
            SortOptionsMenuItem = new MyMenuItem("Sorty By")
            {
                Submenus = SortOptions
            };
        }

        #region Properties

        //Selected File
        private Codex selectedFile;
        public Codex SelectedFile {
            get { return selectedFile; } 
            set { SetProperty(ref selectedFile, value); }
        }

        //Set Type of view
        public Enums.FileView FileViewLayout { get; init; }

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
                    Command = new RelayCommand<string>(MVM.FilterHandler.SortBy),
                    CommandParam = sortOption
                });
            }
        }

        //Open File whereever
        public bool OpenFile(Codex codex = null)
        {
            bool success = Utils.tryFunctions(MVM.SettingsVM.OpenFilePriority, codex);
            if (!success) MessageBox.Show("Could not open file, please check local path or URL");
            return success;
        }

        //Open File Offline
        public ReturningRelayCommand<Codex> OpenFileLocallyCommand { get; init; }
        public static bool OpenFileLocally(Codex toOpen = null)
        {
            if(toOpen == null) toOpen = MVM.CurrentFileViewModel.SelectedFile;
            try
            {
                Process.Start(new ProcessStartInfo(toOpen.Path) {UseShellExecute = true });
                return true;
            }
            catch(Exception ex)
            {
                Logger.log.Error(ex.InnerException);

                //Check if folder exists, if not ask users to rename
                var dir = Path.GetDirectoryName(toOpen.Path);
                if (!Directory.Exists(dir))
                {
                    string message = $"{toOpen.Path} could not be found. \n" +
                    $"If you renamed a folder, go to \n" +
                    $"Settings -> General -> Fix Renamed Folder\n" +
                    $"to update all references to the old folder name.";
                    MessageBox.Show(message, "Path could not be found", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return false;
            }
        }
        public bool CanOpenFileLocally(Codex toOpen = null)
        {
            if (toOpen == null) toOpen = MVM.CurrentFileViewModel.SelectedFile;

            //if SelectedFile is also null
            if (toOpen == null) return false;

            return toOpen.HasOfflineSource();
        }

        //Open File Online
        public ReturningRelayCommand<Codex> OpenFileOnlineCommand { get; init; }
        public static bool OpenFileOnline(Codex toOpen = null)
        {
            if(toOpen == null) toOpen = MVM.CurrentFileViewModel.SelectedFile;

            //fails if no internet, pinging 8.8.8.8 DNS instead of server because some sites like gmbinder block ping
            if (!Utils.pingURL()) return false;

            try
            {
                Process.Start(new ProcessStartInfo(toOpen.SourceURL) { UseShellExecute = true });
                return true;
            }
            catch (Exception ex)
            {
                Logger.log.Error(ex.InnerException);
                return false;
            }

        }
        public bool CanOpenFileOnline(Codex toOpen = null)
        {
            if (toOpen == null) toOpen = MVM.CurrentFileViewModel.SelectedFile;

            //if SelectedFile is also null
            if (toOpen == null) return false;

            return toOpen.HasOnlineSource();
        }

        //Open Multiple Files
        public ReturningRelayCommand<IEnumerable> OpenSelectedFilesCommand { get; init; }
        public bool OpenSelectedFiles(IEnumerable toOpen)
        {
            if (toOpen == null) return false;
            List<Codex> ToOpen = toOpen.Cast<Codex>().ToList();
            //MessageBox "Are you Sure?"
            string sMessageBoxText = "You are about to open " + ToOpen.Count + " Files. Are you sure you wish to continue?";
            string sCaption = "Are you Sure?";

            MessageBoxButton btnMessageBox = MessageBoxButton.YesNo;
            MessageBoxImage imgMessageBox = MessageBoxImage.Warning;

            MessageBoxResult rsltMessageBox = MessageBox.Show(sMessageBoxText, sCaption, btnMessageBox, imgMessageBox);

            if (rsltMessageBox == MessageBoxResult.Yes)
            {
                foreach(Codex f in ToOpen)  OpenFile(f);
                return true;
            }
            else { return false; }
        }

        //Edit File
        public ActionCommand EditFileCommand { get; private set; }
        public void EditFile()
        {
            MVM.CurrentEditViewModel = new FileEditViewModel(MVM.CurrentFileViewModel.SelectedFile);
            FilePropWindow fpw = new FilePropWindow((FileEditViewModel)MVM.CurrentEditViewModel);
            fpw.ShowDialog();
            fpw.Topmost = true;
        }

        //Edit Multiple files
        public RelayCommand<IEnumerable> EditFilesCommand { get; init; }
        public void EditFiles(IEnumerable toEdit)
        {
            if(toEdit == null) return;
            List<Codex> ToEdit = toEdit.Cast<Codex>().ToList();
            MVM.CurrentEditViewModel = new FileBulkEditViewModel(ToEdit);
            FileBulkEditWindow fpw = new FileBulkEditWindow((FileBulkEditViewModel)MVM.CurrentEditViewModel);
            fpw.ShowDialog();
            fpw.Topmost = true;
        }

        //Show in Explorer
        public RelayCommand<Codex> ShowInExplorerCommand { get; init; }
        public void ShowInExplorer(Codex toShow = null)
        {
            if (toShow == null) toShow = MVM.CurrentFileViewModel.SelectedFile;

            string folderPath = Path.GetDirectoryName(toShow.Path);
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                Arguments = folderPath,
                FileName = "explorer.exe"
            };
            Process.Start(startInfo);
        }


        //Move Codex to other CodexCollection
        public RelayCommand<object[]> MoveToFolderCommand { get; init; }
        public void MoveToFolder(object[] par = null)
        {
            //par contains 2 parameters
            List<Codex> ToMoveList = new List<Codex>();
            string targetCollectionName;

            //extract Collection parameter
            if (par[0] != null) targetCollectionName = (string)(par[0]);
            else return;
            if (targetCollectionName == MVM.CurrentFolder) return;

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
        }

        //Delete File
        public RelayCommand<object> DeleteFileCommand { get; init; }
        public void DeleteFile(object o = null)
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
            MVM.Refresh();
        }
        #endregion
    }
}
