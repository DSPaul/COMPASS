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
        public FileEditViewModel(MainViewModel vm, Codex ToEdit) : base(vm)
        {
            EditedCodex = ToEdit;
            //apply all changes to new codex so they can be cancelled, only copy changes over after OK is clicked
            TempCodex = new Codex(MVM.CurrentCollection);
            if(!CreateNewCodex) TempCodex.Copy(EditedCodex);

            //Apply right checkboxes in Alltags
            foreach (TreeViewNode t in AllTreeViewNodes)
            {
                t.Expanded = false;
                t.Selected = TempCodex.Tags.Contains(t.Tag) ? true : false;
                if (t.Children.Any(node => TempCodex.Tags.Contains(node.Tag))) t.Expanded = true;
            }

            //Commands
            BrowsePathCommand = new BasicCommand(BrowsePath);
            TagCheckCommand = new BasicCommand(Update_Taglist);
            DeleteFileCommand = new BasicCommand(DeleteFile);
            BrowseURLCommand = new BasicCommand(BrowseURL);
            RegenArtCommand = new BasicCommand(RegenArt);
            SelectArtCommand = new BasicCommand(SelectArt);
        }

        #region Properties

        readonly Codex EditedCodex;

        private bool CreateNewCodex
        {
            get { return EditedCodex == null; }
        }

        private Codex _tempCodex;
        public Codex TempCodex
        {
            get { return _tempCodex; }
            set { SetProperty(ref _tempCodex, value); }
        }

        #endregion

        #region Funtions and Commands

        public BasicCommand BrowsePathCommand { get; private set; }
        private void BrowsePath()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                AddExtension = false,
                Filter = "PDF (*.pdf) | *.pdf"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                TempCodex.Path = openFileDialog.FileName;
            }
        }

        public override void OKBtn()
        {
            //Copy changes into Codex
            if(!CreateNewCodex) EditedCodex.Copy(TempCodex);
            else
            {
                Codex ToAdd = new Codex();
                ToAdd.Copy(TempCodex);
                MVM.CurrentCollection.AllFiles.Add(ToAdd);
            }
            //Add new Author and Publishers to lists
            if(TempCodex.Author != "" && !MVM.CurrentCollection.AuthorList.Contains(TempCodex.Author)) 
                MVM.CurrentCollection.AuthorList.Add(TempCodex.Author);
            if(TempCodex.Publisher != "" && !MVM.CurrentCollection.PublisherList.Contains(TempCodex.Publisher)) 
                MVM.CurrentCollection.PublisherList.Add(TempCodex.Publisher);

            MVM.Reset();
            CloseAction();
        }

        public override void Cancel()
        {
            CloseAction();
        }

        public BasicCommand TagCheckCommand { get; private set; }
        public void Update_Taglist()
        {
            TempCodex.Tags.Clear();
            foreach (TreeViewNode t in AllTreeViewNodes)
            {
                if (t.Selected)
                {
                    TempCodex.Tags.Add(t.Tag);
                }
            }
        }

        public BasicCommand DeleteFileCommand { get; private set; }
        private void DeleteFile()
        {
            if (!CreateNewCodex)
            {
                MVM.CurrentCollection.DeleteFile(EditedCodex);
                MVM.FilterHandler.RemoveFile(EditedCodex);
            }
            CloseAction();
        }

        public BasicCommand BrowseURLCommand { get; private set; }
        private void BrowseURL()
        {
            if (TempCodex.SourceURL == "") return;
            System.Diagnostics.Process.Start(TempCodex.SourceURL);
        }

        public BasicCommand RegenArtCommand { get; private set; }
        private void RegenArt()
        {
            CoverArtGenerator.ConvertPDF(TempCodex, MVM.CurrentCollection.Folder);
            //force refresh
            string CovArt = TempCodex.CoverArt; 
            TempCodex.CoverArt = null;
            TempCodex.CoverArt = CovArt;
        }

        public BasicCommand SelectArtCommand { get; private set; }
        private void SelectArt()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                AddExtension = false,
                Multiselect = false,
                Filter = "Image files (*.jpg, *.jpeg, *.png) | *.jpg; *.jpeg; *.png"
        };
            if (openFileDialog.ShowDialog() == true)
            {
                CoverArtGenerator.SaveImageAsCover(openFileDialog.FileName, TempCodex);
            }
            //force refresh because it is cached
            string CovArt = TempCodex.CoverArt;
            TempCodex.CoverArt = null;
            TempCodex.CoverArt = CovArt;
        }
        #endregion
    }
}

