using COMPASS.Models;
using COMPASS.ViewModels.Commands;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COMPASS.ViewModels
{
    public class FileEditViewModel : BaseEditViewModel
    {
        public FileEditViewModel(MainViewModel vm) : base(vm)
        {
            EditedFile = MVM.CurrentFileViewModel.SelectedFile;
            TempFile = new MyFile();
            tempFile.Copy(EditedFile);

            //Apply right checkboxes in Alltags
            foreach (TreeViewNode t in AllTreeViewNodes)
            {
                t.Selected = TempFile.Tags.Contains(t.Tag) ? true : false;
            }

            //Commands
            BrowsePathCommand = new BasicCommand(BrowsePath);
            TagCheckCommand = new BasicCommand(Update_Taglist);
            DeleteFileCommand = new BasicCommand(DeleteFile);
            BrowseURLCommand = new BasicCommand(BrowseURL);
        }

        #region Properties

        readonly MyFile EditedFile;

        private MyFile tempFile;
        public MyFile TempFile
        {
            get { return tempFile; }
            set { SetProperty(ref tempFile, value); }
        }

        #endregion

        #region Funtions and Commands

        public BasicCommand BrowsePathCommand { get; private set; }
        private void BrowsePath()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                AddExtension = false
            };
            if (openFileDialog.ShowDialog() == true)
            {
                TempFile.Path = openFileDialog.FileName;
            }
        }

        public override void OKBtn()
        {
            Update_Taglist();
            ////Copy changes into Database
            EditedFile.Copy(TempFile);
            ////Add new Author and Publishers to lists
            if(TempFile.Author != "" && !MVM.CurrentData.AuthorList.Contains(TempFile.Author)) MVM.CurrentData.AuthorList.Add(TempFile.Author);
            if(TempFile.Publisher != "" && !MVM.CurrentData.PublisherList.Contains(TempFile.Publisher)) MVM.CurrentData.PublisherList.Add(TempFile.Publisher);

            MVM.FilterHandler.Update_ActiveFiles();
            CloseAction();
        }

        public BasicCommand TagCheckCommand { get; private set; }
        public void Update_Taglist()
        {
            TempFile.Tags.Clear();
            foreach (TreeViewNode t in AllTreeViewNodes)
            {
                if (t.Selected)
                {
                    TempFile.Tags.Add(t.Tag);
                }
            }
        }

        public BasicCommand DeleteFileCommand { get; private set; }
        private void DeleteFile()
        {
            MVM.CurrentData.DeleteFile(EditedFile);
            MVM.FilterHandler.RemoveFile(EditedFile);
            CloseAction();
        }

        public BasicCommand BrowseURLCommand { get; private set; }
        private void BrowseURL()
        {
            if (TempFile.SourceURL == "") return;
            System.Diagnostics.Process.Start(TempFile.SourceURL);
        }
        #endregion
    }
}

