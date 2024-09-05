using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Models;
using COMPASS.Common.Services;
using COMPASS.Common.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace COMPASS.Common.ViewModels
{
    public class CodexEditViewModel : ViewModelBase, IEditViewModel
    {
        public CodexEditViewModel(Codex? toEdit)
        {
            _editedCodex = toEdit;
            //apply all changes to new codex so they can be cancelled, only copy changes over after OK is clicked
            _tempCodex = new(MainViewModel.CollectionVM.CurrentCollection);
            if (!CreateNewCodex) TempCodex.Copy(_editedCodex!);

            //Apply right checkboxes in AllTags
            foreach (TreeViewNode t in AllTreeViewNodes)
            {
                t.Expanded = false;
                t.Selected = TempCodex.Tags.Contains(t.Tag);
                if (t.Children.Any(node => TempCodex.Tags.Contains(node.Tag))) t.Expanded = true;
            }
        }

        #region Properties

        readonly Codex? _editedCodex;

        private ObservableCollection<TreeViewNode>? _treeViewSource;
        public ObservableCollection<TreeViewNode> TreeViewSource => _treeViewSource ??= new(MainViewModel.CollectionVM.CurrentCollection.RootTags.Select(tag => new TreeViewNode(tag)));

        private HashSet<TreeViewNode> AllTreeViewNodes => TreeViewSource.Flatten().ToHashSet();

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

        //public CreatableLookUpContract Contract { get; set; } = new();

        #endregion

        #region Methods and Commands

        private RelayCommand? _browsePathCommand;
        public RelayCommand BrowsePathCommand => _browsePathCommand ??= new(BrowsePath);
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

        private RelayCommand? _browseURLCommand;
        public RelayCommand BrowseURLCommand => _browseURLCommand ??= new(BrowseURL);
        private void BrowseURL()
        {
            if (CodexViewModel.CanOpenCodexOnline(TempCodex))
            {
                CodexViewModel.OpenCodexOnline(TempCodex);
            }
        }

        private RelayCommand? _browseISBNCommand;
        public RelayCommand BrowseISBNCommand => _browseISBNCommand ??= new(BrowseISBN);
        private void BrowseISBN()
        {
            string url = $"https://openlibrary.org/search?q={TempCodex.ISBN}&mode=everything";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private RelayCommand? _tagCheckCommand;
        public RelayCommand TagCheckCommand => _tagCheckCommand ??= new(UpdateTagList);
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

        private RelayCommand? _quickCreateTagCommand;
        public RelayCommand QuickCreateTagCommand => _quickCreateTagCommand ??= new(QuickCreateTag);
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
                OnPropertyChanged(nameof(TreeViewSource));

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

        private RelayCommand? _deleteCodexCommand;
        public RelayCommand DeleteCodexCommand => _deleteCodexCommand ??= new(DeleteCodex);
        private void DeleteCodex()
        {
            if (!CreateNewCodex)
            {
                CodexViewModel.DeleteCodex(_editedCodex);
            }
            CloseAction();
        }

        private AsyncRelayCommand? _fetchCoverCommand;
        public AsyncRelayCommand FetchCoverCommand => _fetchCoverCommand ??= new(FetchCoverAsync);
        private async Task FetchCoverAsync()
        {
            ShowLoading = true;
            //make it so cover always gets overwritten if this case, store old value first
            CodexProperty coverProp = PreferencesService.GetInstance().Preferences.CodexProperties.First(prop => prop.Name == nameof(Codex.CoverArt));
            MetaDataOverwriteMode curSetting = coverProp.OverwriteMode;
            coverProp.OverwriteMode = MetaDataOverwriteMode.Always;
            //get the cover
            await CoverService.GetCover(TempCodex);
            //Restore cover preference
            coverProp.OverwriteMode = curSetting;
            ShowLoading = false;
            RefreshCover();
        }

        private RelayCommand? _chooseCoverCommand;
        public RelayCommand ChooseCoverCommand => _chooseCoverCommand ??= new(ChooseCover);
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
                CoverService.GetCoverFromImage(openFileDialog.FileName, TempCodex);
                RefreshCover();
            }
        }

        private void RefreshCover()
        {
            //force refresh because image is cached
            string covArt = TempCodex.CoverArt;
            string thumbnail = TempCodex.Thumbnail;
            TempCodex.CoverArt = String.Empty;
            TempCodex.Thumbnail = String.Empty;
            TempCodex.CoverArt = covArt;
            TempCodex.Thumbnail = thumbnail;
        }

        public Action CloseAction { get; set; } = () => { };

        private RelayCommand? _oKCommand;
        public RelayCommand OKCommand => _oKCommand ??= new(OKBtn);
        public void OKBtn()
        {
            //Copy changes into Codex
            if (!CreateNewCodex)
            {
                _editedCodex!.Copy(TempCodex);
            }
            else
            {
                Codex toAdd = new();
                toAdd.Copy(TempCodex);
                MainViewModel.CollectionVM.CurrentCollection.AllCodices.Add(toAdd);
            }

            MainViewModel.CollectionVM.CurrentCollection.Save();

            //Add new Authors, Publishers, ect. to metadata lists
            MainViewModel.CollectionVM.FilterVM.PopulateMetaDataCollections();
            MainViewModel.CollectionVM.FilterVM.ReFilter();
            CloseAction();
        }

        private RelayCommand? _cancelCommand;
        public RelayCommand CancelCommand => _cancelCommand ??= new(Cancel);
        public void Cancel() => CloseAction();


        public void OnDragOver(object sender, DragEventArgs e)
        {
            if (e.Data is DataObject data
                && data.GetFiles()?.Count() == 1
                && IOService.IsImageFile(data.GetFiles()?.Select(f => f.Path.AbsolutePath).First() ?? ""))
            {
                e.DragEffects = DragDropEffects.Copy;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
        }

        public void Drop(object sender, DragEventArgs e)
        {
            if (e.Data is DataObject data
                && data.GetFiles()?.Count() == 1
                && IOService.IsImageFile(data.GetFiles()?.Select(f => f.Path.AbsolutePath).First() ?? ""))
            {
                string path = data.GetFiles()!.Select(f => f.Path.AbsolutePath).First();
                CoverService.GetCoverFromImage(path, TempCodex);
                RefreshCover();
            }
        }
        #endregion
    }
}

