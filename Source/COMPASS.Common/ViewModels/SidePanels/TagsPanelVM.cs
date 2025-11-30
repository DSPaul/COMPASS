using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Filters;
using COMPASS.Common.Models.Hierarchy;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.Modals.Edit;
using COMPASS.Common.ViewModels.Modals.Import;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Common.ViewModels.Selection;
using COMPASS.Common.Views.Windows;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.ViewModels.SidePanels
{
    public class TagsPanelVM : ViewModelBase, IDealsWithTabControl
    {
        public TagsPanelVM(CodexCollectionVM codexCollectionVm, FiltersViewModel filtersVM)
        {
            _codexCollectionVm = codexCollectionVm;

            codexCollectionVm.Collection.PropertyChanged += OnCollectionChanged;
            
            _filtersVM = filtersVM;
            UpdateTagsAsTreeNodes();
        }

        private readonly CodexCollectionVM _codexCollectionVm;
        private readonly FiltersViewModel _filtersVM;

        #region Properties
        
        //Selected tab from tabControl with options to add tags
        private int _selectedTab = 0;
        public int SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (value > 0) Collapsed = false;
                switch (value)
                {
                    case 1:
                        AddTag();
                        break;
                    case 2:
                        AddGroup();
                        break;
                }
                PrevSelectedTab = _selectedTab;
                SetProperty(ref _selectedTab, value);
            }
        }

        public int PrevSelectedTab { get ; set ; }
        
        private bool _collapsed = false;
        public bool Collapsed
        {
            get => _collapsed;
            set
            {
                SetProperty(ref _collapsed, value);
                if (value) SelectedTab = 0;
            }
        }

        private bool _modeIsInclude = true;
        public bool ModeIsInclude
        {
            get => _modeIsInclude;
            set => SetProperty(ref _modeIsInclude, value);
        }
        
        //TreeViewSource with hierarchy
        private ObservableCollection<TreeNode<TagViewModel>> _tagsAsTreeNodes = [];
        public ObservableCollection<TreeNode<TagViewModel>> TagsAsTreeNodes
        {
            get => _tagsAsTreeNodes;
            set => SetProperty(ref _tagsAsTreeNodes, value);
        }
        
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
            
            //Update the tree when a tag switches parent
            foreach (TreeNode<TagViewModel> node in newNodes)
            {
                node.Item.PropertyChanged += OnTagParentChanged;
            }
            
            TagsAsTreeNodes = new(tagsAsTreeNodes);
        }

        #region Commands
        //Tag Creation ViewModel
        private TagEditViewModel? _addTagViewModel;
        public TagEditViewModel? AddTagViewModel
        {
            get => _addTagViewModel;
            set => SetProperty(ref _addTagViewModel, value);
        }

        //Group Creation ViewModel
        private TagEditViewModel? _addGroupViewModel;
        public TagEditViewModel? AddGroupViewModel
        {
            get => _addGroupViewModel;
            set => SetProperty(ref _addGroupViewModel, value);
        }

        //Add Tag Buttons
        private RelayCommand? _addTagCommand;
        public RelayCommand AddTagCommand => _addTagCommand ??= new(AddTag);
        public void AddTag() => AddTagViewModel = new TagEditViewModel(new Tag(), _codexCollectionVm, true);


        private RelayCommand? _addGroupCommand;
        public RelayCommand AddGroupCommand => _addGroupCommand ??= new(AddGroup);
        public void AddGroup()
        {
            Tag newTag = new()
            {
                IsGroup = true,
            };
            AddGroupViewModel = new TagEditViewModel(newTag, _codexCollectionVm, true);
        }

        private RelayCommand<TagViewModel?>? _addTagFilterCommand;
        public RelayCommand<TagViewModel?> AddTagFilterCommand => _addTagFilterCommand ??= new(AddTagFilterHelper);
        private void AddTagFilterHelper(TagViewModel? tagVm)
        {
            if (tagVm != null)
            {
                _filtersVM.ActivateFilter(new TagFilter(tagVm), ModeIsInclude);
            }
        }

        private RelayCommand? _importTagsFromOtherCollectionsCommand;
        public RelayCommand ImportTagsFromOtherCollectionsCommand => _importTagsFromOtherCollectionsCommand ??= new(ImportTagsFromOtherCollections);
        public void ImportTagsFromOtherCollections()
        {
            var importVM = new ImportTagsViewModel(CollectionManager.CollectionNames);
            var w = new ModalWindow(importVM);
            w.Show();
        }

        private AsyncRelayCommand? _importTagsFromSatchelCommand;
        public AsyncRelayCommand ImportTagsFromSatchelCommand => _importTagsFromSatchelCommand ??= new(ImportTagsFromSatchel);
        public async Task ImportTagsFromSatchel()
        {
            var collectionStorageService = ServiceResolver.ResolveKeyed<ICodexCollectionStorageService>(StorageStrategy.Xml);
            var collectionToImport = await collectionStorageService.OpenSatchel();

            if (collectionToImport == null)
            {
                Logger.Warn("Failed to open file");
                return;
            }

            var importVM = new ImportTagsViewModel(collectionToImport);

            if (!importVM.TagsSelectorVM.HasTags)
            {
                Notification noTagsFound = new("No Tags found", $"{collectionToImport[2..]} does not contain tags");
                await ServiceResolver.Resolve<INotificationService>().ShowDialog(noTagsFound);
                return;
            }

            var w = new ModalWindow(importVM);
            w.Show();
        }

        private RelayCommand? _exportTagsCommand;
        public RelayCommand ExportTagsCommand => _exportTagsCommand ??= new(ExportTags);
        public void ExportTags()
        {
            var vm = new ExportCollectionViewModel
            {
                //configure export vm for tags only
                AdvancedExport = true
            };

            vm.Steps.Clear();
            vm.Steps.Add(CollectionContentSelectorViewModel.TagsStep);
            foreach (var codex in vm.ContentSelectorVM.SelectableCodices)
            {
                codex.Selected = false;
            }

            var w = new ExportCollectionWizard(vm);
            w.Show();
        }

        #endregion
        
        #region Drag & Drop Tags Treeview
        //Drop on Treeview Behaviour
        //TODO: used to have to call the default implementation of drag and drop here, not sure 
        void OnDrop(object sender, DragEventArgs e)
        {
            // Drag & Drop will modify the Collection of Treeview nodes that the treeview is bound to
            // We need to convert that back to the collection of Tags so that the changes are saved
            var newRootTags = TagsAsTreeNodes.Select(ToTag).ToList();

            foreach (Tag? t in newRootTags)
            {
                t!.Parent = null;
            }

            // Cannot do TreeRoot = ExtractTagsFromTreeViewSource(TreeViewSource); because that changes ref of TreeRoot
            _codexCollectionVm.Collection.RootTags.Clear();
            _codexCollectionVm.Collection.RootTags.AddRange(newRootTags);
        }
        
        //TODO move this somewhere else
        private Tag ToTag(TreeNode<TagViewModel> node)
        {
            Tag tag = node.Item.GetModel();
            
            //add children according to treeview
            tag.Children = new(node.Children.Select(ToTag));

            //set parentID for all the children
            foreach (Tag childTag in tag.Children)
            {
                childTag.Parent = tag;
            }

            return tag;
        }
        #endregion

        #region Tag Context Menu
        private AsyncRelayCommand<TagViewModel?>? _createChildCommand;
        public AsyncRelayCommand<TagViewModel?> CreateChildCommand => _createChildCommand ??= new(CreateChildTag);
        private async Task CreateChildTag(TagViewModel? referenceTag)
        {
            if (referenceTag is not null)
            {
                Tag newTag = new(ActiveCollection.AllTags)
                {
                    Parent = referenceTag.GetModel()
                };
                ModalWindow modal = new(new TagEditViewModel(newTag, _codexCollectionVm, true));
                await modal.ShowDialog(App.MainWindow);
            }
        }

        private RelayCommand<TagViewModel?>? _sortChildrenCommand;
        public RelayCommand<TagViewModel?> SortChildrenCommand => _sortChildrenCommand ??= new(SortChildren, CanSortChildren);
        private void SortChildren(TagViewModel? parentTag)
        {
            RecursiveSortChildren(parentTag?.GetModel());
            UpdateTagsAsTreeNodes();
        }
        private void RecursiveSortChildren(Tag? tag)
        {
            if (tag is null) return;
            tag.Children = new(tag.Children.OrderBy(t => t.Name));
            foreach (Tag child in tag.Children)
            {
                RecursiveSortChildren(child);
            }
        }
        
        private bool CanSortChildren(TagViewModel? tagVm) => tagVm?.Children.Any() == true;

        private RelayCommand? _sortAllTagsCommand;
        public RelayCommand SortAllTagsCommand => _sortAllTagsCommand ??= new(SortAllTags);
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

        private AsyncRelayCommand<TagViewModel?>? _editTagCommand;
        public AsyncRelayCommand<TagViewModel?> EditTagCommand => _editTagCommand ??= new(EditTag);
        private async Task EditTag(TagViewModel? toEdit)
        {
            if (toEdit is null) return;
            ModalWindow modal = new(new TagEditViewModel(toEdit.GetModel(), _codexCollectionVm, false));
            await modal.ShowDialog(App.MainWindow);
        }

        private RelayCommand<TagViewModel?>? _deleteTagCommand;
        public RelayCommand<TagViewModel?> DeleteTagCommand => _deleteTagCommand ??= new(DeleteTag);

        private void DeleteTag(TagViewModel? toDelete)
        {
            if (toDelete is null) return;
            
            _codexCollectionVm.Collection.DeleteTag(toDelete.GetModel());
            _filtersVM.RemoveFilter(ModelVmFactory.GetFilterViewModel(new TagFilter(toDelete)));

            _codexCollectionVm.Collection.Save();

            UpdateTagsAsTreeNodes();
        }
        #endregion
    }
}
