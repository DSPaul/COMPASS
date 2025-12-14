using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Exceptions;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Hierarchy;
using COMPASS.Common.Operations;
using COMPASS.Common.Services;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Common.Views.Windows;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.ViewModels.Modals.Edit
{
    public class CodexEditViewModel : EditViewModelBase<CodexViewModel, Codex>
    {
        public CodexEditViewModel(Codex sourceCodex, bool createNew = false, CollectionTabVM? tabVm = null) 
            : base(sourceCodex, createNew, codex => new CodexViewModel(codex, (tabVm ?? TabsViewModel.GetInstance().ActiveTab)!.CollectionVM))
        {
            TabVM = tabVm ?? TabsViewModel.GetInstance().ActiveTab ?? throw new NoTabException("An active tab is expected when editing a codex");
        
            var publisherList = TabVM.FiltersVM.PublisherList;
            PublisherOptions = ["", ..publisherList];

            WorkingCopy.LoadCover();
            AuthorList = TabVM.FiltersVM.AuthorList.ToList();
            
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
    
        protected ObservableCollection<CheckableTreeNode<TagViewModel>>? _allTagsAsTreeNodes;
        public ObservableCollection<CheckableTreeNode<TagViewModel>> AllTagsAsTreeNodes => _allTagsAsTreeNodes ??= 
            new(TabVM.CollectionVM.Collection.RootTags
                .Select(TabVM.CollectionVM.GetTagVm)
                .Select(tagVm => new CheckableTreeNode<TagViewModel>(tagVm)));

        protected HashSet<CheckableTreeNode<TagViewModel>> AllTreeNodes => AllTagsAsTreeNodes.Flatten().ToHashSet();
    
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
            if (CodexOperations.CanOpenCodexOnline(WorkingCopy.GetModel()))
            {
                CodexOperations.OpenCodexOnline(WorkingCopy.GetModel());
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
            WorkingCopy.GetModel().Tags.Clear();
            WorkingCopy.GetModel().Tags.AddRange(AllTagsAsTreeNodes.Flatten().Where(node => node.IsChecked == true).Select(node => node.Item.GetModel()));
        }

        private AsyncRelayCommand? _quickCreateTagCommand;
        public AsyncRelayCommand QuickCreateTagCommand => _quickCreateTagCommand ??= new(QuickCreateTag);
        public async Task QuickCreateTag()
        {
            //keep track of the count to check if tags were created
            int tagCount = TabVM.CollectionVM.Collection.AllTags.Count;

            TagEditViewModel tagEditVm = new(new Tag(), TabVM.CollectionVM, createNew: true);
            await WindowManager.OpenModal(tagEditVm);
            
            if (TabVM.CollectionVM.Collection.AllTags.Count > tagCount) //new tag was created
            {
                //recalculate treeview source
                _allTagsAsTreeNodes = null;
                OnPropertyChanged(nameof(AllTagsAsTreeNodes));

                //Apply right checkboxes in AllTags
                foreach (CheckableTreeNode<TagViewModel> t in AllTreeNodes)
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
                await CodexOperations.DeleteCodex(_source);
            }
            
            CloseAction();
        }

        private AsyncRelayCommand? _fetchCoverCommand;
        public AsyncRelayCommand FetchCoverCommand => _fetchCoverCommand ??= new(FetchCoverAsync);
        private async Task FetchCoverAsync()
        {
            ShowLoading = true;
            //make it so cover always gets overwritten if this case, store old value first
            CodexProperty coverProp = PreferencesService.GetInstance().Preferences.ImportableCodexProperties.First(prop => prop.Name == nameof(SourceMetaData.Cover));
            MetaDataOverwriteMode curSetting = coverProp.OverwriteMode;
            coverProp.OverwriteMode = MetaDataOverwriteMode.Always;
            //get the cover
            try
            {
                await CoverService.GetAndApplyCover(WorkingCopy.GetModel());
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
                    await CoverService.SaveCover(WorkingCopy.GetModel(), img);
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
                && FileFormatUtils.IsImageFile(data.GetFiles()?.Select(f => f.Path.AbsolutePath).First() ?? ""))
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
                && FileFormatUtils.IsImageFile(data.GetFiles()?.Select(f => f.Path.AbsolutePath).First() ?? ""))
            {
                string path = data.GetFiles()!.Select(f => f.Path.AbsolutePath).First();
                var img = CoverService.GetCoverFromImage(path);
                if (img != null)
                {
                    await CoverService.SaveCover(WorkingCopy.GetModel(), img);
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

