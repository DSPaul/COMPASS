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
            _editedCodex = toEdit;
            //apply all changes to new codex so they can be cancelled, only copy changes over after OK is clicked
            TempCodex = new(MainViewModel.CollectionVM.CurrentCollection);
            if (!CreateNewCodex) TempCodex.Copy(_editedCodex);

            //Apply right checkboxes in AllTags
            foreach (TreeViewNode t in AllTreeViewNodes)
            {
                t.Expanded = false;
                t.Selected = TempCodex.Tags.Contains(t.Tag);
                if (t.Children.Any(node => TempCodex.Tags.Contains(node.Tag))) t.Expanded = true;
            }
        }

        #region Properties

        readonly Codex _editedCodex;

        private ObservableCollection<TreeViewNode> _treeViewSource;
        public ObservableCollection<TreeViewNode> TreeViewSource => _treeViewSource ??= new(MainViewModel.CollectionVM.CurrentCollection.RootTags.Select(tag => new TreeViewNode(tag)));

        private HashSet<TreeViewNode> AllTreeViewNodes => Utils.FlattenTree(TreeViewSource).ToHashSet();

        private bool CreateNewCodex => _editedCodex == null;

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
                InitialDirectory = Path.GetDirectoryName(TempCodex.Path) ?? String.Empty
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
            {
                CodexViewModel.OpenCodexOnline(TempCodex);
            }
        }

        private ActionCommand _browseISBNCommand;
        public ActionCommand BrowseISBNCommand => _browseISBNCommand ??= new(BrowseISBN);
        private void BrowseISBN()
        {
            string url = $"https://openlibrary.org/search?q={TempCodex.ISBN}&mode=everything";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private ActionCommand _tagCheckCommand;
        public ActionCommand TagCheckCommand => _tagCheckCommand ??= new(UpdateTagList);
        private void UpdateTagList()
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
            //keep track of count to check of tags were created
            int tagCount = MainViewModel.CollectionVM.CurrentCollection.RootTags.Count;

            TagEditViewModel tagEditVM = new(null, createNew: true);
            TagPropWindow tpw = new(tagEditVM)
            {
                Topmost = true
            };
            _ = tpw.ShowDialog();

            if (MainViewModel.CollectionVM.CurrentCollection.RootTags.Count > tagCount) //new tag was created
            {
                //recalculate treeview source
                _treeViewSource = null;
                RaisePropertyChanged(nameof(TreeViewSource));

                //Apply right checkboxes in AllTags
                foreach (TreeViewNode t in AllTreeViewNodes)
                {
                    t.Expanded = false;
                    t.Selected = TempCodex.Tags.Contains(t.Tag);
                    if (t.Children.Any(node => TempCodex.Tags.Contains(node.Tag))) t.Expanded = true;
                }

                //check the newly created tag
                TreeViewSource.Last().Selected = true;

                UpdateTagList();
            }
        }

        private ActionCommand _deleteCodexCommand;
        public ActionCommand DeleteCodexCommand => _deleteCodexCommand ??= new(DeleteCodex);
        private void DeleteCodex()
        {
            if (!CreateNewCodex)
            {
                CodexViewModel.DeleteCodex(_editedCodex);
            }
            CloseAction();
        }

        private ActionCommand _fetchCoverCommand;
        public ActionCommand FetchCoverCommand => _fetchCoverCommand ??= new(FetchCover);
        private async void FetchCover()
        {
            ShowLoading = true;
            //make it so cover always gets overwritten if this case, store old value first
            CodexProperty coverProp = SettingsViewModel.GetInstance().MetaDataPreferences.First(prop => prop.Label == "Cover Art");
            Debug.Assert(coverProp.OverwriteMode != null, "coverProp.OverwriteMode != null");
            MetaDataOverwriteMode curSetting = (MetaDataOverwriteMode)coverProp.OverwriteMode;
            coverProp.OverwriteMode = MetaDataOverwriteMode.Always;
            //get the cover
            await Task.Run(() => CoverFetcher.GetCover(TempCodex));
            //Restore cover preference
            coverProp.OverwriteMode = curSetting;
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
            string covArt = TempCodex.CoverArt;
            string thumbnail = TempCodex.Thumbnail;
            TempCodex.CoverArt = null;
            TempCodex.Thumbnail = null;
            TempCodex.CoverArt = covArt;
            TempCodex.Thumbnail = thumbnail;
        }

        public Action CloseAction { get; set; }

        private ActionCommand _oKCommand;
        public ActionCommand OKCommand => _oKCommand ??= new(OKBtn);
        public void OKBtn()
        {
            //Copy changes into Codex
            if (!CreateNewCodex)
            {
                _editedCodex.Copy(TempCodex);
            }
            else
            {
                Codex toAdd = new();
                toAdd.Copy(TempCodex);
                MainViewModel.CollectionVM.CurrentCollection.AllCodices.Add(toAdd);
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

