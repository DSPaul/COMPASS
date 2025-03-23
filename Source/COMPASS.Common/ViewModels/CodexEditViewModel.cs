using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Models;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Operations;
using COMPASS.Common.Services;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Modals;
using COMPASS.Common.Views.Windows;

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

            TempCodex.LoadCover();

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

        private AsyncRelayCommand? _browsePathCommand;
        public AsyncRelayCommand BrowsePathCommand => _browsePathCommand ??= new(BrowsePath);
        private async Task BrowsePath()
        {
            var filesService = App.Container.Resolve<IFilesService>();

            var files = await filesService.OpenFilesAsync(new()
            {
                //TODO, this needs to be a folder, not a path
                //SuggestedStartLocation = Path.GetDirectoryName(TempCodex.Sources.Path) ?? string.Empty
            });

            if (files.Any())
            {
                using var file = files.Single();
                TempCodex.Sources.Path = file.Path.AbsolutePath;
            }
        }

        private RelayCommand? _browseURLCommand;
        public RelayCommand BrowseURLCommand => _browseURLCommand ??= new(BrowseURL);
        private void BrowseURL()
        {
            if (CodexOperations.CanOpenCodexOnline(TempCodex))
            {
                CodexOperations.OpenCodexOnline(TempCodex);
            }
        }

        private RelayCommand? _browseISBNCommand;
        public RelayCommand BrowseISBNCommand => _browseISBNCommand ??= new(BrowseISBN);
        private void BrowseISBN()
        {
            string url = $"https://openlibrary.org/search?q={TempCodex.Sources.ISBN}&mode=everything";
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

        private AsyncRelayCommand? _quickCreateTagCommand;
        public AsyncRelayCommand QuickCreateTagCommand => _quickCreateTagCommand ??= new(QuickCreateTag);
        public async Task QuickCreateTag()
        {
            //keep track of count to check of tags were created
            int tagCount = MainViewModel.CollectionVM.CurrentCollection.RootTags.Count;

            TagEditViewModel tagEditVm = new(null, createNew: true);
            var modal = new ModalWindow(tagEditVm);
            await modal.ShowDialog(App.MainWindow);

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

        private AsyncRelayCommand? _deleteCodexCommand;
        public AsyncRelayCommand DeleteCodexCommand => _deleteCodexCommand ??= new(DeleteCodex);
        private async Task DeleteCodex()
        {
            if (!CreateNewCodex)
            {
                await CodexOperations.DeleteCodex(_editedCodex);
            }
            CloseAction();
        }

        private AsyncRelayCommand? _fetchCoverCommand;
        public AsyncRelayCommand FetchCoverCommand => _fetchCoverCommand ??= new(FetchCoverAsync);
        private async Task FetchCoverAsync()
        {
            ShowLoading = true;
            //make it so cover always gets overwritten if this case, store old value first
            CodexProperty coverProp = PreferencesService.GetInstance().Preferences.CodexProperties.First(prop => prop.Name == nameof(Codex.CoverArtPath));
            MetaDataOverwriteMode curSetting = coverProp.OverwriteMode;
            coverProp.OverwriteMode = MetaDataOverwriteMode.Always;
            //get the cover
            try
            {
                await CoverService.GetCover(TempCodex);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                //Restore cover preference
                coverProp.OverwriteMode = curSetting;
                ShowLoading = false;
            }
            TempCodex.LoadCover();
        }

        private AsyncRelayCommand? _chooseCoverCommand;
        public AsyncRelayCommand ChooseCoverCommand => _chooseCoverCommand ??= new(ChooseCover);
        private async Task ChooseCover()
        {
            var filesService = App.Container.Resolve<IFilesService>();

            var files = await filesService.OpenFilesAsync(new()
            {
                FileTypeFilter = [FilePickerFileTypes.ImageAll],
            });

            if (files.Any())
            {
                using var file = files.Single();
                CoverService.GetCoverFromImage(file.Path.AbsolutePath, TempCodex);
                TempCodex.LoadCover();
            }
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
                TempCodex.LoadCover();
            }
        }
        #endregion
    }
}

