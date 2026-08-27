using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Operations;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Filters;
using COMPASS.Common.Models.Hierarchy;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.Modals;
using COMPASS.Common.ViewModels.Modals.Edit;
using COMPASS.Common.ViewModels.Modals.Import;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Common.Views.Windows;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Interfaces.Services;
using COMPASS.Infra.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace COMPASS.Common.ViewModels.SidePanels
{
    public class TagsPanelVM : ViewModelBase
    {
        private readonly ILogger _logger;
        private readonly IImportExportService _importExportService;
        private readonly INotificationService _notificationService;
        private readonly TagOperations _tagOperations;
        private readonly CodexCollectionVMFactory _codexCollectionVMFactory;
        private readonly ExportCollectionViewModelFactory _exportCollectionViewModelFactory;
        private readonly TagEditViewModelFactory _tagEditViewModelFactory;
        private readonly TagViewModelFactory _tagViewModelFactory;
        private readonly CodexCollectionVM _codexCollectionVm;
        private readonly FiltersViewModel _filtersVM;

        public TagsPanelVM(
            ILogger logger,
            IImportExportService importExportService,
            INotificationService notificationService,
            TagOperations tagOperations,
            CodexCollectionVMFactory codexCollectionVMFactory,
            ExportCollectionViewModelFactory exportCollectionViewModelFactory,
            TagEditViewModelFactory tagEditViewModelFactory,
            TagViewModelFactory tagViewModelFactory,
            CodexCollectionVM codexCollectionVm,
            FiltersViewModel filtersVM)
        {
            _logger = logger;
            _importExportService = importExportService;
            _notificationService = notificationService;
            _tagOperations = tagOperations;
            _codexCollectionVMFactory = codexCollectionVMFactory;
            _exportCollectionViewModelFactory = exportCollectionViewModelFactory;
            _tagEditViewModelFactory = tagEditViewModelFactory;
            _tagViewModelFactory = tagViewModelFactory;
            _codexCollectionVm = codexCollectionVm;

            codexCollectionVm.Collection.PropertyChanged += OnCollectionChanged;
            
            _filtersVM = filtersVM;
            UpdateTagsAsTreeNodes();
        }

        #region Properties
        
        //Selected tab from tabControl with options to add tags
        public int SelectedTab
        {
            get;
            set
            {
                switch (value)
                {
                    case 0:
                        AddTag();
                        break;
                    case 1:
                        AddGroup();
                        break;
                }

                SetProperty(ref field, value);
            }
        } = -1;

        public bool ModeIsInclude
        {
            get;
            set => SetProperty(ref field, value);
        } = true;

        //TreeViewSource with hierarchy
        public ObservableCollection<TreeNode<TagViewModel>> TagsAsTreeNodes
        {
            get;
            set => SetProperty(ref field, value);
        } = [];

        #endregion

        private void OnCollectionChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CodexCollection.RootTags) ||
                e.PropertyName == nameof(CodexCollection.AllTags))
            {
                UpdateTagsAsTreeNodes();
            }
        }
        
        private void OnTagParentChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TagViewModel.Parent))
            {
                UpdateTagsAsTreeNodes();
            }
        }
        
        public void UpdateTagsAsTreeNodes()
        {
            List<TreeNode<TagViewModel>> tagsAsTreeNodes = 
                _codexCollectionVm.Collection.RootTags
                    .Select(_codexCollectionVm.GetTagVm)
                    .Select(tagVm => new TreeNode<TagViewModel>(tagVm))
                    .ToList();
            
            List<TreeNode<TagViewModel>> newNodes = tagsAsTreeNodes.Flatten().ToList();

            // transfer expanded property
            if (TagsAsTreeNodes.Any())
            {
                var oldNodes = TagsAsTreeNodes.Flatten().ToList();
                foreach (TreeNode<TagViewModel> node in oldNodes)
                {
                    node.Item.PropertyChanged -= OnTagParentChanged;
                }

                foreach (TreeNode<TagViewModel> newNode in newNodes)
                {
                    newNode.Expanded = oldNodes.Find(n => n.Item == newNode.Item)?.Expanded ?? newNode.Expanded;
                }
            }
            else
            {
                foreach (TreeNode<TagViewModel> newNode in newNodes)
                {
                    newNode.Expanded = newNode.Item.IsGroup;
                }
            }
            
            //Update the tree when a tag switches parent
            foreach (TreeNode<TagViewModel> node in newNodes)
            {
                node.Item.PropertyChanged += OnTagParentChanged;
            }
            
            TagsAsTreeNodes = new(tagsAsTreeNodes);
        }

        #region Commands
        //Tag Creation ViewModel
        public TagEditViewModel? AddTagViewModel
        {
            get;
            set => SetProperty(ref field, value);
        }

        //Group Creation ViewModel
        public TagEditViewModel? AddGroupViewModel
        {
            get;
            set => SetProperty(ref field, value);
        }

        //Add Tag Buttons
        public RelayCommand AddTagCommand => field ??= new(AddTag);
        public void AddTag() => AddTagViewModel = _tagEditViewModelFactory.Create(new Tag(), _codexCollectionVm, true);


        public RelayCommand AddGroupCommand => field ??= new(AddGroup);
        public void AddGroup()
        {
            Tag newTag = new()
            {
                IsGroup = true,
            };
            AddGroupViewModel = _tagEditViewModelFactory.Create(newTag, _codexCollectionVm, true);
        }

        public RelayCommand<TagViewModel?> AddTagFilterCommand => field ??= new(AddTagFilterHelper);
        private void AddTagFilterHelper(TagViewModel? tagVm)
        {
            if (tagVm != null)
            {
                _filtersVM.ActivateFilter(new TagFilter(tagVm), ModeIsInclude);
            }
        }

        public AsyncRelayCommand ImportTagsFromOtherCollectionsCommand => field ??= new(ImportTagsFromOtherCollections);
        public async Task ImportTagsFromOtherCollections()
        {
            var importVM = new ImportTagsViewModel(_tagViewModelFactory,
                (IEnumerable<string>)CollectionManager.CollectionNames, ActiveCollection.Name);
            await WindowManager.OpenModal(importVM);
        }

        public AsyncRelayCommand ImportTagsFromSatchelCommand => field ??= new(ImportTagsFromSatchel);
        public async Task ImportTagsFromSatchel()
        {
            var importCollection = await _importExportService.OpenSatchel();

            if (importCollection == null)
            {
                _logger.Warn("Failed to open file");
                return;
            }

            if (!importCollection.RootTags.Any())
            {
                Notification noTagsFound = new("No Tags found", $"{importCollection.Name[2..]} does not contain tags");
                await _notificationService.ShowDialog(noTagsFound);
                return;
            }
            
            using CodexCollectionVM toImportVm = _codexCollectionVMFactory.Create(importCollection, StorageStrategy.Xml);
            var tagImportVM = new ImportTagsViewModel(_tagViewModelFactory, toImportVm, ActiveCollection.Name);

            var w = new ModalWindow(tagImportVM);
            await w.ShowDialog(WindowManager.ActiveWindow);

            //Delete satchel when done
            toImportVm.DeleteCollection();
        }

        public AsyncRelayCommand ExportTagsCommand => field ??= new(ExportTags);
        public async Task ExportTags()
        {
            var vm = _exportCollectionViewModelFactory.CreateTagsExporter(_codexCollectionVm.Collection);

            var w = new ModalWindow(vm);
            await w.ShowDialog(WindowManager.ActiveWindow);
        }

        #endregion

        #region Tag Context Menu

        public AsyncRelayCommand<TagViewModel?> CreateChildCommand => field ??= new(CreateChildTag);
        private async Task CreateChildTag(TagViewModel? referenceTag)
        {
            if (referenceTag is not null)
            {
                Tag newTag = new(ActiveCollection.AllTags)
                {
                    Parent = referenceTag.GetModel()
                };
                var vm = _tagEditViewModelFactory.Create(newTag, _codexCollectionVm, true);
                await WindowManager.OpenModal(vm);
            }
        }

        public RelayCommand<TagViewModel?> SortChildrenCommand => field ??= new(SortChildren, CanSortChildren);
        private void SortChildren(TagViewModel? parentTag)
        {
            RecursiveSortChildren(parentTag?.GetModel());
            UpdateTagsAsTreeNodes();
        }
        private void RecursiveSortChildren(Tag? tag)
        {
            if (tag is null) return;
            tag.Children.ReplaceRange(tag.Children.OrderBy(t => t.Name).ToList());
            foreach (Tag child in tag.Children)
            {
                RecursiveSortChildren(child);
            }
        }
        
        private bool CanSortChildren(TagViewModel? tagVm) => tagVm?.Children.Any() == true;

        public RelayCommand SortAllTagsCommand => field ??= new(SortAllTags);
        private void SortAllTags()
        {
            Tag t = new()
            {
                Children = new(_codexCollectionVm.Collection.RootTags)
            };
            RecursiveSortChildren(t);
            ActiveCollection.RootTags = t.Children.ToList();
            UpdateTagsAsTreeNodes();
        }

        public AsyncRelayCommand<TagViewModel?> EditTagCommand => field ??= new(EditTag);
        private async Task EditTag(TagViewModel? toEdit)
        {
            if (toEdit is null) return;
            var vm = _tagEditViewModelFactory.Create(toEdit.GetModel(), _codexCollectionVm, false);
            await WindowManager.OpenModal(vm);
        }

        public AsyncRelayCommand<TagViewModel?> DeleteTagCommand => field ??= new(DeleteTag);

        private async Task DeleteTag(TagViewModel? toDelete)
        {
            if (toDelete is null) return;
            
            bool deleted = await _tagOperations.Delete(_codexCollectionVm.Collection, toDelete.GetModel());
            if (!deleted) return;
            _filtersVM.RemoveFilter(ModelVmFactory.GetFilterViewModel(new TagFilter(toDelete)));

            _codexCollectionVm.Collection.Save();

            UpdateTagsAsTreeNodes();
        }
        #endregion
    }

    [Factory]
    public class TagsPanelVMFactory(
        ILogger logger,
        IImportExportService importExportService,
        INotificationService notificationService,
        TagOperations tagOperations,
        CodexCollectionVMFactory codexCollectionVMFactory,
        ExportCollectionViewModelFactory exportCollectionViewModelFactory,
        TagEditViewModelFactory tagEditViewModelFactory,
        TagViewModelFactory tagViewModelFactory)
    {
        public TagsPanelVM Create(CodexCollectionVM collectionVm, FiltersViewModel filtersVm)
            => new(logger, importExportService, notificationService, tagOperations, codexCollectionVMFactory, exportCollectionViewModelFactory, tagEditViewModelFactory,
                   tagViewModelFactory, collectionVm, filtersVm);
    }
}
