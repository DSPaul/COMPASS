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
        public FileEditViewModel(Codex ToEdit) : base()
        {
            EditedCodex = ToEdit;
            //apply all changes to new codex so they can be cancelled, only copy changes over after OK is clicked
            TempCodex = new Codex(CurrentCollection);
            if(!CreateNewCodex) TempCodex.Copy(EditedCodex);

            //Apply right checkboxes in Alltags
            foreach (TreeViewNode t in AllTreeViewNodes)
            {
                t.Expanded = false;
                t.Selected = TempCodex.Tags.Contains(t.Tag) ? true : false;
                if (t.Children.Any(node => TempCodex.Tags.Contains(node.Tag))) t.Expanded = true;
            }

            //Commands
            BrowsePathCommand = new ActionCommand(BrowsePath);
            TagCheckCommand = new ActionCommand(Update_Taglist);
            DeleteFileCommand = new ActionCommand(DeleteFile);
            BrowseURLCommand = new ActionCommand(BrowseURL);
            FetchCoverCommand = new ActionCommand(FetchCover);
            ChooseCoverCommand = new ActionCommand(ChooseCover);
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

        private bool CoverArtChanged = false;

        #endregion

        #region Funtions and Commands

        public ActionCommand BrowsePathCommand { get; private set; }
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
                CurrentCollection.AllFiles.Add(ToAdd);
                MVM.Refresh();
            }
            //Add new Author and Publishers to lists
            if(TempCodex.Author != "" && !CurrentCollection.AuthorList.Contains(TempCodex.Author)) 
                CurrentCollection.AuthorList.Add(TempCodex.Author);
            if(TempCodex.Publisher != "" && !CurrentCollection.PublisherList.Contains(TempCodex.Publisher)) 
                CurrentCollection.PublisherList.Add(TempCodex.Publisher);

            //reset needed to show art update
            if(CoverArtChanged) MVM.Refresh();
            CloseAction();
        }

        public override void Cancel()
        {
            CloseAction();
        }

        public ActionCommand TagCheckCommand { get; private set; }
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

        public ActionCommand DeleteFileCommand { get; private set; }
        private void DeleteFile()
        {
            if (!CreateNewCodex)
            {
                CurrentCollection.DeleteFile(EditedCodex);
                MVM.FilterHandler.RemoveFile(EditedCodex);
            }
            CloseAction();
        }

        public ActionCommand BrowseURLCommand { get; private set; }
        private void BrowseURL()
        {
            if (TempCodex.SourceURL == "") return;
            System.Diagnostics.Process.Start(TempCodex.SourceURL);
        }

        public ActionCommand FetchCoverCommand { get; private set; }
        private void FetchCover()
        {
            CoverFetcher.GetCover(TempCodex);
            refreshCover();
        }

        public ActionCommand ChooseCoverCommand { get; private set; }
        private void ChooseCover()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                AddExtension = false,
                Multiselect = false,
                Filter = "Image files (*.jpg, *.jpeg, *.png) | *.jpg; *.jpeg; *.png"
        };
            if (openFileDialog.ShowDialog() == true)
            {
                CoverFetcher.GetCoverFromImage(openFileDialog.FileName, TempCodex);
                refreshCover();
            }
        }

        private void refreshCover()
        {
            //force refresh because image is cached
            string CovArt = TempCodex.CoverArt;
            string Thumbn = TempCodex.Thumbnail;
            TempCodex.CoverArt = null;
            TempCodex.Thumbnail = null;
            TempCodex.CoverArt = CovArt;
            TempCodex.Thumbnail = Thumbn;

            CoverArtChanged = true;
        }
        #endregion
    }
}

