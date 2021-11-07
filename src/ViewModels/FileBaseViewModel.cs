using COMPASS.Models;
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
            EditFileCommand = new BasicCommand(EditFile);
            EditFilesCommand = new RelayCommand<object>(EditFiles);
            OpenSelectedFileCommand = new RelayCommand<object>(OpenSelectedFile, CanOpenSelectedFile);
            OpenFileOnlineCommand = new RelayCommand<object>(OpenFileOnline,CanOpenFileOnline);
            MoveToFolderCommand = new RelayCommand<object>(MoveToFolder);
            DeleteFileCommand = new RelayCommand<object>(DeleteFile);
            OpenSelectedFilesCommand = new RelayCommand<object>(OpenSelectedFiles);

            ViewOptions = new ObservableCollection<MyMenuItem>();
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

        private ObservableCollection<MyMenuItem> viewOptions;
        public ObservableCollection<MyMenuItem> ViewOptions
        {
            get { return viewOptions; }
            set { SetProperty(ref viewOptions, value); }
        }
        #endregion

        #region Functions and Commands

        //Open File Offline
        public RelayCommand<object> OpenSelectedFileCommand { get; private set; }
        public void OpenSelectedFile(object o = null)
        {
            Codex ToOpen = o != null ? (Codex)o : MVM.CurrentFileViewModel.SelectedFile;
            try
            {
                Process.Start(new ProcessStartInfo(ToOpen.Path) {UseShellExecute = true });
            }
            catch
            {
                MessageBox.Show("File Path Invalid");
            }
        }
        public bool CanOpenSelectedFile(object o = null)
        {
            Codex ToOpen = o != null ? (Codex)o : MVM.CurrentFileViewModel.SelectedFile;

            //if SelectedFile is also null
            if (ToOpen == null) return false;

            return ToOpen.HasOfflineSource();
        }

        //Open File online
        public RelayCommand<object> OpenFileOnlineCommand { get; private set; }
        public void OpenFileOnline(object o = null)
        {
            Codex ToOpen = o != null ? (Codex)o : MVM.CurrentFileViewModel.SelectedFile;

            try
            {
                Process.Start(new ProcessStartInfo(ToOpen.SourceURL) { UseShellExecute = true });
            }
            catch
            {
                MessageBox.Show("URL Invalid");
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
        public void OpenSelectedFiles(object o = null)
        {
            if (o == null) return;
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
            }
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
        public void EditFiles(object o = null)
        {
            if (o == null) return;
            IList list = o as IList;
            List<Codex> ToEdit = list.Cast<Codex>().ToList();
            MVM.CurrentEditViewModel = new FileBulkEditViewModel(MVM, ToEdit);
            FileBulkEditWindow fpw = new FileBulkEditWindow((FileBulkEditViewModel)MVM.CurrentEditViewModel);
            fpw.ShowDialog();
            fpw.Topmost = true;
        }

        //Move Codex to other CodexCollection
        public RelayCommand<object> MoveToFolderCommand { get; private set; }
        public void MoveToFolder(object o = null)
        {
            //par contains 2 parameters
            var par = (object[])o;
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
                    ToMove.ID = TargetCollection.GetAvailableID();

                    //Add Codex to target CodexCollection
                    TargetCollection.AllFiles.Add(ToMove);

                    //Update Author and Publisher List
                    if (ToMove.Author != "" && !TargetCollection.AuthorList.Contains(ToMove.Author)) TargetCollection.AuthorList.Add(ToMove.Author);
                    if (ToMove.Publisher != "" && !TargetCollection.PublisherList.Contains(ToMove.Publisher)) TargetCollection.PublisherList.Add(ToMove.Publisher);

                    //Move cover art to right folder with new ID
                    string newCoverArt = CodexCollection.CollectionsPath + targetCollectionName + @"\CoverArt\" + ToMove.ID + ".png";
                    File.Copy(ToMove.CoverArt, newCoverArt);

                    //Delete file in original folder
                    MVM.CurrentCollection.DeleteFile(ToMove);
                    MVM.FilterHandler.RemoveFile(ToMove);

                    //Update the cover art metadata to new path, has to happen after delete so old one gets deleted
                    ToMove.CoverArt = newCoverArt;
                }
                //Save changes to TargetCollection
                TargetCollection.SaveFilesToFile();
            }
        }

        //Delete File
        public RelayCommand<object> DeleteFileCommand { get; private set; }
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
            
        }
        #endregion
    }
}
