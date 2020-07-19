using COMPASS.Models;
using COMPASS.ViewModels.Commands;
using System;
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
            OpenSelectedFileCommand = new RelayCommand<object>(OpenSelectedFile, CanOpenSelectedFile);
            OpenFileOnlineCommand = new RelayCommand<object>(OpenFileOnline,CanOpenFileOnline);
            MoveToFolderCommand = new RelayCommand<object>(MoveToFolder);
            DeleteFileCommand = new BasicCommand(DeleteFile);
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

        //Edit File
        public BasicCommand EditFileCommand { get; private set; }
        public void EditFile()
        {
            MVM.CurrentEditViewModel = new FileEditViewModel(MVM, MVM.CurrentFileViewModel.SelectedFile);
            FilePropWindow fpw = new FilePropWindow((FileEditViewModel)MVM.CurrentEditViewModel);
            fpw.ShowDialog();
            fpw.Topmost = true;
        }

        //Move File to other folder
        public RelayCommand<object> MoveToFolderCommand { get; private set; }
        public void MoveToFolder(object o = null)
        {
            var par = (object[])o;
            MyFile ToMove;
            string targetfolder;

            //extract folder parameter
            if (par[0] != null) targetfolder = (string)(par[0]);
            else return;
            if (targetfolder == MVM.CurrentFolder) return;

            //extract file parameter
            if (par[1] != null) ToMove = (MyFile)(par[1]);
            else ToMove = MVM.CurrentFileViewModel.SelectedFile;

            //MessageBox "Are you Sure?"
            string sMessageBoxText = "Moving " + ToMove.Title + " to " + targetfolder + " will remove all tags from the file, are you sure you wish to continue?";
            string sCaption = "Are you Sure?";

            MessageBoxButton btnMessageBox = MessageBoxButton.YesNo;
            MessageBoxImage imgMessageBox = MessageBoxImage.Warning;

            MessageBoxResult rsltMessageBox = MessageBox.Show(sMessageBoxText, sCaption, btnMessageBox, imgMessageBox);

            if (rsltMessageBox == MessageBoxResult.Yes) 
            {
                ToMove.Tags.Clear();
                // Give file new ID and move it to other folder
                Data TargetData = new Data(targetfolder);
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

                //Save changes to TargetData
                TargetData.SaveFilesToFile();
            }
        }

        //Delete File
        public BasicCommand DeleteFileCommand { get; private set; }
        public void DeleteFile()
        {
            MVM.CurrentData.DeleteFile(MVM.CurrentFileViewModel.SelectedFile);
            MVM.FilterHandler.RemoveFile(MVM.CurrentFileViewModel.SelectedFile);
        }

        #endregion
    }
}
