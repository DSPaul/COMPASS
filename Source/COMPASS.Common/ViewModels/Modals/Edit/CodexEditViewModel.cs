using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Exceptions;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Hierarchy;
using COMPASS.Common.Operations;
using COMPASS.Common.Services;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.Views.Windows;
using COMPASS.Infra.ExtensionMethods;

namespace COMPASS.Common.ViewModels.Modals.Edit
{
    public class CodexEditViewModel : EditViewModelBase<Codex>
    {
        public CodexEditViewModel(Codex sourceCodex, bool createNew = false, CollectionTabVM? tabVm = null) 
            : base(sourceCodex, createNew)
        {
            TabVM = tabVm ?? TabsViewModel.GetInstance().ActiveTab ?? throw new NoTabException("An active tab is expected when editing a codex");
        
            var publisherList = TabVM.FilterVM.PublisherList;
            PublisherOptions = ["", ..publisherList];

            WorkingCopy.LoadCover();
            AuthorList = TabVM.FilterVM.AuthorList.ToList();
            
            //Apply right checkboxes in AllTags
            foreach (var node in AllTreeNodes)
            {
                node.Expanded = false;
                node.IsChecked = WorkingCopy.Tags.Contains(node.Item);
                if (node.Children.Any(n => WorkingCopy.Tags.Contains(n.Item)))
                {
                    node.Expanded = true;
                }
            }
        }
        
        #region Properties
    
        public CollectionTabVM TabVM { get; }
    
        protected ObservableCollection<CheckableTreeNode<Tag>>? _allTagsAsTreeNodes;
        public ObservableCollection<CheckableTreeNode<Tag>> AllTagsAsTreeNodes => _allTagsAsTreeNodes ??= 
            new(TabVM.CollectionVM.Collection.RootTags.Select(tag => new CheckableTreeNode<Tag>(tag)));

        protected HashSet<CheckableTreeNode<Tag>> AllTreeNodes => AllTagsAsTreeNodes.Flatten().ToHashSet();
    
        public List<string> PublisherOptions { get; }

        #endregion
        
        #region Properties

        private bool _showLoading = false;
        public bool ShowLoading
        {
            get => _showLoading;
            set => SetProperty(ref _showLoading, value);
        }

        public List<string> AuthorList { get; }
        
        #endregion

        #region Methods and Commands
        
        //TODO this doesn't work, must be on codex itself, make a vm for it
        protected override void CustomValidate(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                ValidatePageCount();
                return;
            }
            
            switch (propertyName)
            {
                case nameof(Codex.PageCount):
                    ValidatePageCount();
                    break;
            }
        }

        private void ValidatePageCount()
        {
            if (WorkingCopy.PageCount < 0)
            {
                AddError(nameof(Codex.PageCount), "Pagecount must be a positive number.");
            }
        }

        protected override void HandleCreateNew(Codex newCodex)
        {
            newCodex.Collection.AllCodices.Add(newCodex);
        }

        protected override void BeforeApply(Codex source, Codex proposal)
        {
            //Nothing to do
        }

        protected override void OnApplied(Codex source)
        {
            source.Collection.Save();
        }

        private AsyncRelayCommand? _browsePathCommand;
        public AsyncRelayCommand BrowsePathCommand => _browsePathCommand ??= new(BrowsePath);
        private async Task BrowsePath()
        {
            var filesService = ServiceResolver.Resolve<IFilesService>();

            var files = await filesService.OpenFilesAsync(new()
            {
                //TODO, this needs to be a folder, not a path
                //SuggestedStartLocation = Path.GetDirectoryName(WorkingCopy.Sources.Path) ?? string.Empty
            });

            if (files.Any())
            {
                using var file = files.Single();
                WorkingCopy.Sources.Path = file.Path.AbsolutePath;
            }
        }

        private RelayCommand? _browseURLCommand;
        public RelayCommand BrowseURLCommand => _browseURLCommand ??= new(BrowseURL);
        private void BrowseURL()
        {
            if (CodexOperations.CanOpenCodexOnline(WorkingCopy))
            {
                CodexOperations.OpenCodexOnline(WorkingCopy);
            }
        }

        private RelayCommand? _browseISBNCommand;
        public RelayCommand BrowseISBNCommand => _browseISBNCommand ??= new(BrowseISBN);
        private void BrowseISBN()
        {
            string url = $"https://openlibrary.org/search?q={WorkingCopy.Sources.ISBN}&mode=everything";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private RelayCommand? _tagCheckCommand;
        public RelayCommand TagCheckCommand => _tagCheckCommand ??= new(UpdateTagList);
        private void UpdateTagList()
        {
            WorkingCopy.Tags.Clear();
            WorkingCopy.Tags.AddRange(CheckableTreeNode<Tag>.GetCheckedItems(AllTagsAsTreeNodes));
        }

        private AsyncRelayCommand? _quickCreateTagCommand;
        public AsyncRelayCommand QuickCreateTagCommand => _quickCreateTagCommand ??= new(QuickCreateTag);
        public async Task QuickCreateTag()
        {
            //keep track of the count to check of tags were created
            int tagCount = WorkingCopy.Collection.RootTags.Count;

            TagEditViewModel tagEditVm = new(new Tag(), createNew: true);
            var modal = new ModalWindow(tagEditVm);
            await modal.ShowDialog(App.MainWindow); //TODO make this the window of the codex edit

            //TODO, we can now create tags outside of root, this is not longer correct
            if (WorkingCopy.Collection.RootTags.Count > tagCount) //new tag was created
            {
                //recalculate treeview source
                _allTagsAsTreeNodes = null;
                OnPropertyChanged(nameof(AllTagsAsTreeNodes));

                //Apply right checkboxes in AllTags
                foreach (CheckableTreeNode<Tag> t in AllTreeNodes)
                {
                    t.Expanded = false;
                    t.IsChecked = WorkingCopy.Tags.Contains(t.Item);
                    if (t.Children.Any(node => WorkingCopy.Tags.Contains(node.Item))) t.Expanded = true;
                }

                //check the newly created tag
                AllTagsAsTreeNodes.Last().IsChecked = true;

                UpdateTagList();
            }
        }

        private AsyncRelayCommand? _deleteCodexCommand;
        public AsyncRelayCommand DeleteCodexCommand => _deleteCodexCommand ??= new(DeleteCodex);
        private async Task DeleteCodex()
        {
            if (!_createNew)
            {
                await TabVM.CodexCommands.DeleteCodex(_source);
            }
            
            CloseAction();
        }

        private AsyncRelayCommand? _fetchCoverCommand;
        public AsyncRelayCommand FetchCoverCommand => _fetchCoverCommand ??= new(FetchCoverAsync);
        private async Task FetchCoverAsync()
        {
            ShowLoading = true;
            //make it so cover always gets overwritten if this case, store old value first
            CodexProperty coverProp = PreferencesService.GetInstance().Preferences.ImportableCodexProperties.First(prop => prop.Name == nameof(Codex.Cover));
            MetaDataOverwriteMode curSetting = coverProp.OverwriteMode;
            coverProp.OverwriteMode = MetaDataOverwriteMode.Always;
            //get the cover
            try
            {
                await CoverService.GetAndApplyCover(WorkingCopy);
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
            WorkingCopy.LoadCover();
        }

        private AsyncRelayCommand? _chooseCoverCommand;
        public AsyncRelayCommand ChooseCoverCommand => _chooseCoverCommand ??= new(ChooseCover);
        private async Task ChooseCover()
        {
            var filesService = ServiceResolver.Resolve<IFilesService>();

            var files = await filesService.OpenFilesAsync(new()
            {
                FileTypeFilter = [FilePickerFileTypes.ImageAll],
            });

            if (files.Any())
            {
                using var file = files.Single();
                var img = CoverService.GetCoverFromImage(file.Path.AbsolutePath);
                if (img != null)
                {
                    await CoverService.SaveCover(WorkingCopy, img);
                }
                WorkingCopy.LoadCover();
            }
        }

        #endregion

        #region  Drag and Drop
        
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

        public async void Drop(object sender, DragEventArgs e)
        {
            if (e.Data is DataObject data
                && data.GetFiles()?.Count() == 1
                && IOService.IsImageFile(data.GetFiles()?.Select(f => f.Path.AbsolutePath).First() ?? ""))
            {
                string path = data.GetFiles()!.Select(f => f.Path.AbsolutePath).First();
                var img = CoverService.GetCoverFromImage(path);
                if (img != null)
                {
                    await CoverService.SaveCover(WorkingCopy, img);
                }
                WorkingCopy.LoadCover();
            }
        }
        #endregion

        #region IModalViewModel

        public override string WindowTitle => "Edit item properties";
        
        #endregion

        public override void Dispose()
        {
            base.Dispose();
            WorkingCopy.Dispose();
        }
    }
}

