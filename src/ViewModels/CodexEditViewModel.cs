using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.Windows;
using GongSolutions.Wpf.DragDrop;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace COMPASS.ViewModels
{
    public class CodexEditViewModel : ViewModelBase, IEditViewModel, IDropTarget
    {
        public CodexEditViewModel(Codex toEdit)
        {
            EditedCodex = toEdit;
            //apply all changes to new codex so they can be cancelled, only copy changes over after OK is clicked
            TempCodex = new(MainViewModel.CollectionVM.CurrentCollection);
            if (!CreateNewCodex) TempCodex.Copy(EditedCodex);

            //Apply right checkboxes in Alltags
            foreach (TreeViewNode t in AllTreeViewNodes)
            {
                t.Expanded = false;
                t.Selected = TempCodex.Tags.Contains(t.Tag);
                if (t.Children.Any(node => TempCodex.Tags.Contains(node.Tag))) t.Expanded = true;
            }
        }

        #region Properties

        readonly Codex EditedCodex;

        private ObservableCollection<TreeViewNode> _treeViewSource;
        public ObservableCollection<TreeViewNode> TreeViewSource => _treeViewSource ??= new(MainViewModel.CollectionVM.CurrentCollection.RootTags.Select(tag => new TreeViewNode(tag)));

        private HashSet<TreeViewNode> AllTreeViewNodes => Utils.FlattenTree(TreeViewSource).ToHashSet();

        private bool CreateNewCodex => EditedCodex == null;

        private Codex _tempCodex;
        public Codex TempCodex
        {
            get => _tempCodex;
            set => SetProperty(ref _tempCodex, value);
        }

        private bool _showLoading = false;
        public bool ShowLoading
        {
            get => _showLoading;
            set => SetProperty(ref _showLoading, value);
        }

        public CreatableLookUpContract Contract { get; set; } = new();

        #endregion

        #region Methods and Commands

        private ActionCommand _browsePathCommand;
        public ActionCommand BrowsePathCommand => _browsePathCommand ??= new(BrowsePath);
        private void BrowsePath()
        {
            OpenFileDialog openFileDialog = new()
            {
                AddExtension = false,
                InitialDirectory = Path.GetDirectoryName(TempCodex.Path)
            };
            if (openFileDialog.ShowDialog() == true)
            {
                TempCodex.Path = openFileDialog.FileName;
            }
        }

        private ActionCommand _browseURLCommand;
        public ActionCommand BrowseURLCommand => _browseURLCommand ??= new(BrowseURL);
        private void BrowseURL()
        {
            if (CodexViewModel.CanOpenCodexOnline(TempCodex))
                CodexViewModel.OpenCodexOnline(TempCodex);
        }

        private ActionCommand _browseISBNCommand;
        public ActionCommand BrowseISBNCommand => _browseISBNCommand ??= new(BrowseISBN);
        private void BrowseISBN()
        {
            string url = $"https://openlibrary.org/search?q={TempCodex.ISBN}&mode=everything";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private ActionCommand _tagCheckCommand;
        public ActionCommand TagCheckCommand => _tagCheckCommand ??= new(Update_Taglist);
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

        private ActionCommand _quickCreateTagCommand;
        public ActionCommand QuickCreateTagCommand => _quickCreateTagCommand ??= new(QuickCreateTag);
        public void QuickCreateTag()
        {
            TagPropWindow tpw = new(new TagEditViewModel(null, true));
            tpw.Topmost = true;
            _ = tpw.ShowDialog();

            //recalc treeviewsource
            _treeViewSource = null;
            RaisePropertyChanged(nameof(TreeViewSource));
        }

        private ActionCommand _deleteCodexCommand;
        public ActionCommand DeleteCodexCommand => _deleteCodexCommand ??= new(DeleteCodex);
        private void DeleteCodex()
        {
            if (!CreateNewCodex)
            {
                CodexViewModel.DeleteCodex(EditedCodex);
            }
            CloseAction();
        }

        private ActionCommand _fetchCoverCommand;
        public ActionCommand FetchCoverCommand => _fetchCoverCommand ??= new(FetchCover);
        private async void FetchCover()
        {
            ShowLoading = true;
            await Task.Run(() => CoverFetcher.GetCover(TempCodex));
            ShowLoading = false;
            RefreshCover();
        }

        private ActionCommand _chooseCoverCommand;
        public ActionCommand ChooseCoverCommand => _chooseCoverCommand ??= new(ChooseCover);
        private void ChooseCover()
        {
            OpenFileDialog openFileDialog = new()
            {
                AddExtension = false,
                Multiselect = false,
                Filter = "Image files (*.jpg, *.jpeg, *.png) | *.jpg; *.jpeg; *.png"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                CoverFetcher.GetCoverFromImage(openFileDialog.FileName, TempCodex);
                RefreshCover();
            }
        }

        private void RefreshCover()
        {
            //force refresh because image is cached
            string CovArt = TempCodex.CoverArt;
            string Thumbn = TempCodex.Thumbnail;
            TempCodex.CoverArt = null;
            TempCodex.Thumbnail = null;
            TempCodex.CoverArt = CovArt;
            TempCodex.Thumbnail = Thumbn;
        }

        public Action CloseAction { get; set; }

        private ActionCommand _oKCommand;
        public ActionCommand OKCommand => _oKCommand ??= new(OKBtn);
        public void OKBtn()
        {
            //Copy changes into Codex
            if (!CreateNewCodex)
            {
                EditedCodex.Copy(TempCodex);
            }
            else
            {
                Codex ToAdd = new();
                ToAdd.Copy(TempCodex);
                MainViewModel.CollectionVM.CurrentCollection.AllCodices.Add(ToAdd);
            }
            //Add new Authors, Publishers, ect. to metadata lists
            MainViewModel.CollectionVM.FilterVM.PopulateMetaDataCollections();
            MainViewModel.CollectionVM.FilterVM.ReFilter();
            CloseAction();
        }

        private ActionCommand _cancelCommand;
        public ActionCommand CancelCommand => _cancelCommand ??= new(Cancel);
        public void Cancel() => CloseAction();


        public void DragOver(IDropInfo dropInfo)
        {
            if (dropInfo.Data is DataObject data
                && data.GetFileDropList().Count == 1
                && Utils.IsImageFile(data.GetFileDropList().Cast<string>().First()))
            {

                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                dropInfo.Effects = DragDropEffects.Copy;
            }
            else
            {
                dropInfo.Effects = DragDropEffects.None;

            }

        }

        public void Drop(IDropInfo dropInfo)
        {
            if (dropInfo.Data is DataObject data
                && data.GetFileDropList().Count == 1
                && Utils.IsImageFile(data.GetFileDropList().Cast<string>().First()))
            {
                string path = data.GetFileDropList().Cast<string>().First();
                CoverFetcher.GetCoverFromImage(path, TempCodex);
                RefreshCover();
            }
        }
        #endregion
    }
}

