using COMPASS.Models;
using COMPASS.ViewModels.Commands;
using Org.BouncyCastle.Asn1.Crmf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Xceed.Wpf.Toolkit.PropertyGrid;
using static COMPASS.Tools.Enums;

namespace COMPASS.ViewModels
{
    public class FileBaseViewModel : BaseViewModel
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
        private MyFile selectedFile;
        public MyFile SelectedFile {
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
            MyFile ToOpen;
            if (o != null) ToOpen = (MyFile)o;
            else ToOpen = MVM.CurrentFileViewModel.SelectedFile;
            try
            {
                Process.Start(ToOpen.Path);
            }
            catch
            {
                MessageBox.Show("File Path Invalid");
            }
        }
        public bool CanOpenSelectedFile(object o = null)
        {
            MyFile ToOpen;
            if (o != null) ToOpen = (MyFile)o;
            else ToOpen = MVM.CurrentFileViewModel.SelectedFile;

            if (ToOpen == null) return false;
            if (File.Exists(ToOpen.Path)) return true;
            return false;
        }

        //Open File online
        public RelayCommand<object> OpenFileOnlineCommand { get; private set; }
        public void OpenFileOnline(object o = null)
        {
            MyFile ToOpen;
            if (o != null) ToOpen = (MyFile)o;
            else ToOpen = MVM.CurrentFileViewModel.SelectedFile;
            try
            {
                Process.Start(ToOpen.SourceURL);
            }
            catch
            {
                MessageBox.Show("URL Invalid");
            }
            
        }
        public bool CanOpenFileOnline(object o = null)
        {
            MyFile ToOpen;
            if (o != null) ToOpen = (MyFile)o;
            else ToOpen = MVM.CurrentFileViewModel.SelectedFile;

            if (ToOpen == null) return false;
            if (ToOpen.SourceURL == null || ToOpen.SourceURL == "") return false;
            return true;
        }

        //Open Multiple Files
        public RelayCommand<object> OpenSelectedFilesCommand { get; private set; }
        public void OpenSelectedFiles(object o = null)
        {
            if (o == null) return;
            IList list = o as IList;
            List<MyFile> ToOpen = list.Cast<MyFile>().ToList();
            //MessageBox "Are you Sure?"
            string sMessageBoxText = "You are about to open " + ToOpen.Count + " Files. Are you sure you wish to continue?";
            string sCaption = "Are you Sure?";

            MessageBoxButton btnMessageBox = MessageBoxButton.YesNo;
            MessageBoxImage imgMessageBox = MessageBoxImage.Warning;

            MessageBoxResult rsltMessageBox = MessageBox.Show(sMessageBoxText, sCaption, btnMessageBox, imgMessageBox);

            if (rsltMessageBox == MessageBoxResult.Yes)
            {
                foreach(MyFile f in ToOpen)
                {
                    try
                    {
                        if(f.Path != null) Process.Start(f.Path);
                        else Process.Start(f.SourceURL);
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
            List<MyFile> ToEdit = list.Cast<MyFile>().ToList();
            MVM.CurrentEditViewModel = new FileBulkEditViewModel(MVM, ToEdit);
            FileBulkEditWindow fpw = new FileBulkEditWindow((FileBulkEditViewModel)MVM.CurrentEditViewModel);
            fpw.ShowDialog();
            fpw.Topmost = true;
        }

        //Move File to other folder
        public RelayCommand<object> MoveToFolderCommand { get; private set; }
        public void MoveToFolder(object o = null)
        {
            var par = (object[])o;
            List<MyFile> ToMoveList = new List<MyFile>();
            string targetfolder;

            //extract folder parameter
            if (par[0] != null) targetfolder = (string)(par[0]);
            else return;
            if (targetfolder == MVM.CurrentFolder) return;

            //extract file parameter
            if (par[1] != null)
            {
                IList list = par[1] as IList;
                ToMoveList = list.Cast<MyFile>().ToList();
            }
            else ToMoveList.Add(MVM.CurrentFileViewModel.selectedFile);

            //MessageBox "Are you Sure?"
            string MessageSingle = "Moving " + ToMoveList[0].Title + " to " + targetfolder + " will remove all tags from the file, are you sure you wish to continue?";
            string MessageMultiple = "Moving these " + ToMoveList.Count() + " files to " + targetfolder + " will remove all tags from the files, are you sure you wish to continue?";

            string sCaption = "Are you Sure?";
            string sMessageBoxText = ToMoveList.Count == 1 ? MessageSingle : MessageMultiple;

            MessageBoxButton btnMessageBox = MessageBoxButton.YesNo;
            MessageBoxImage imgMessageBox = MessageBoxImage.Warning;

            MessageBoxResult rsltMessageBox = MessageBox.Show(sMessageBoxText, sCaption, btnMessageBox, imgMessageBox);

            if (rsltMessageBox == MessageBoxResult.Yes) 
            {
                Data TargetData = new Data(targetfolder);
                foreach (MyFile ToMove in ToMoveList)
                {
                    ToMove.Tags.Clear();
                    // Give file new ID and move it to other folder
                    MyFile GetIDfile = new MyFile(TargetData); //create new file in target data to check the first available ID
                    ToMove.ID = GetIDfile.ID;

                    //Add file to target dataset
                    TargetData.AllFiles.Add(ToMove);

                    //Update Author and Publisher List
                    if (ToMove.Author != "" && !TargetData.AuthorList.Contains(ToMove.Author)) TargetData.AuthorList.Add(ToMove.Author);
                    if (ToMove.Publisher != "" && !TargetData.PublisherList.Contains(ToMove.Publisher)) TargetData.PublisherList.Add(ToMove.Publisher);

                    //Move cover art to right folder with new ID
                    string newCoverArt = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Compass\Collections\" + targetfolder + @"\CoverArt\" + ToMove.ID + ".png";
                    File.Copy(ToMove.CoverArt, newCoverArt);

                    //Delete file in original folder
                    MVM.CurrentData.DeleteFile(ToMove);
                    MVM.FilterHandler.RemoveFile(ToMove);

                    //Update the cover art metadata to new path, has to happen after delete so old one gets deleted
                    ToMove.CoverArt = newCoverArt;
                }
                //Save changes to TargetData
                TargetData.SaveFilesToFile();
            }
        }

        //Delete File
        public RelayCommand<object> DeleteFileCommand { get; private set; }
        public void DeleteFile(object o = null)
        {
            List<MyFile> ToDeleteList = new List<MyFile>();
            if (o == null) ToDeleteList.Add(MVM.CurrentFileViewModel.SelectedFile);
            else
            {
                IList list = o as IList;
                ToDeleteList = list.Cast<MyFile>().ToList();
            }
            foreach(MyFile ToDelete in ToDeleteList)
            {
                MVM.CurrentData.DeleteFile(ToDelete);
                MVM.FilterHandler.RemoveFile(ToDelete);
            }
            
        }
        #endregion
    }
}
