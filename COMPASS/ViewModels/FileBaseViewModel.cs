using COMPASS.ViewModels.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static COMPASS.Tools.Enums;

namespace COMPASS.ViewModels
{
    public class FileBaseViewModel : BaseViewModel
    {
        public FileBaseViewModel(MainViewModel vm)
        {
            mainViewModel = vm;
            EditFileCommmand = new SimpleCommand(EditFile);
        }

        #region Properties

        //MainViewModel
        private MainViewModel mainViewModel;
        public MainViewModel MVM
        {
            get { return mainViewModel; }
            set { SetProperty(ref mainViewModel, value); }
        }

        public ObservableCollection<MyFile> ActiveFiles
        {
            get { return MVM.FilterHandler.ActiveFiles; }
            set { }  
        }

        private MyFile selectedFile;
        public MyFile SelectedFile {
            get { return selectedFile; } 
            set { SetProperty(ref selectedFile, value); }
        }

        #endregion

        #region Functions and Commands

        public void OpenSelectedFile()
        {
            Process.Start(SelectedFile.Path);
        }

        //Edit File
        public SimpleCommand EditFileCommmand { get; set; }
        public void EditFile(object a = null)
        {
            MVM.CurrentEditViewModel = new FileEditViewModel(MVM);
            FilePropWindow fpw = new FilePropWindow(MVM.CurrentEditViewModel);
            fpw.Show();
        }

        #endregion
    }
}
