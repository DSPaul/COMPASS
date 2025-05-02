using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Filters;
using COMPASS.Common.Models.Hierarchy;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.ViewModels.Modals.Edit;
using COMPASS.Common.Views.Windows;

namespace COMPASS.Common.ViewModels.SidePanels
{
    public class TagsPanelVM : ViewModelBase, IDealsWithTabControl
    {
        public TagsPanelVM(CodexCollection codexCollection, FilterViewModel filterVM)
        {
            _codexCollection = codexCollection;
            _filterVM = filterVM;
            UpdateTagsAsTreeNodes();
        }

        private readonly CodexCollection _codexCollection;
        private readonly FilterViewModel _filterVM;

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
        private ObservableCollection<TreeNode<Tag>> _tagsAsTreeNodes = [];
        public ObservableCollection<TreeNode<Tag>> TagsAsTreeNodes
        {
            get => _tagsAsTreeNodes;
            set => SetProperty(ref _tagsAsTreeNodes, value);
        }
        
        #endregion

        private void OnTagParentChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Tag.Parent))
            {
                UpdateTagsAsTreeNodes();
            }
        }
        
        public void UpdateTagsAsTreeNodes()
        {
            List<TreeNode<Tag>> tagsAsTreeNodes = _codexCollection.RootTags.Select(tag => new TreeNode<Tag>(tag)).ToList();
            List<TreeNode<Tag>> newNodes = tagsAsTreeNodes.Flatten().ToList();

            // transfer expanded property
            if (TagsAsTreeNodes.Any())
            {
                var oldNodes = TagsAsTreeNodes.Flatten().ToList();
                foreach (TreeNode<Tag> node in oldNodes)
                {
                    node.Item.PropertyChanged -= OnTagParentChanged;
                }
                
                foreach (TreeNode<Tag> newNode in newNodes)
                {
                    newNode.Expanded = oldNodes.Find(n => n.Item == newNode.Item)?.Expanded ?? newNode.Expanded;
                }
            }
            
            //Update the tree when a tag switches parent
            foreach (TreeNode<Tag> node in newNodes)
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
        public void AddTag() => AddTagViewModel = new TagEditViewModel(null, true);


        private RelayCommand? _addGroupCommand;
        public RelayCommand AddGroupCommand => _addGroupCommand ??= new(AddGroup);
        public void AddGroup()
        {
            Tag newTag = new()
            {
                IsGroup = true,
            };
            AddGroupViewModel = new TagEditViewModel(newTag, true);
        }

        private RelayCommand<Tag?>? _addTagFilterCommand;
        public RelayCommand<Tag?> AddTagFilterCommand => _addTagFilterCommand ??= new(AddTagFilterHelper);
        public void AddTagFilterHelper(Tag? tag)
        {
            if (tag != null)
            {
                _filterVM.AddFilter(new TagFilter(tag), ModeIsInclude);
            }
        }

        private RelayCommand? _importTagsFromOtherCollectionsCommand;
        public RelayCommand ImportTagsFromOtherCollectionsCommand => _importTagsFromOtherCollectionsCommand ??= new(ImportTagsFromOtherCollections);
        public void ImportTagsFromOtherCollections()
        {
            var importVM = new ImportTagsViewModel(MainViewModel.CollectionVM.AllCodexCollections.ToList());
            var w = new ImportTagsWindow(importVM);
            w.Show();
        }

        private AsyncRelayCommand? _importTagsFromSatchelCommand;
        public AsyncRelayCommand ImportTagsFromSatchelCommand => _importTagsFromSatchelCommand ??= new(ImportTagsFromSatchel);
        public async Task ImportTagsFromSatchel()
        {
            var collectionStorageService = App.Container.Resolve<ICodexCollectionStorageService>();
            var collectionToImport = await collectionStorageService.OpenSatchel();

            if (collectionToImport == null)
            {
                Logger.Warn("Failed to open file");
                return;
            }

            var importVM = new ImportTagsViewModel(collectionToImport);

            if (!importVM.TagsSelectorVM.HasTags)
            {
                Notification noTagsFound = new("No Tags found", $"{collectionToImport.Name[2..]} does not contain tags");
                await App.Container.Resolve<INotificationService>().ShowDialog(noTagsFound);
                return;
            }

            var w = new ImportTagsWindow(importVM);
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
        //TODO: used to have to call the default implemenation of drag and drop here, not sure 
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
            _codexCollection.RootTags.Clear();
            _codexCollection.RootTags.AddRange(newRootTags);
        }
        
        //TODO move this somewhere else
        private Tag ToTag(TreeNode<Tag> node)
        {
            Tag tag = node.Item;
            
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
        private AsyncRelayCommand<Tag?>? _createChildCommand;
        public AsyncRelayCommand<Tag?> CreateChildCommand => _createChildCommand ??= new(CreateChildTag);
        private async Task CreateChildTag(Tag? referenceTag)
        {
            if (referenceTag is not null)
            {
                Tag newTag = new(MainViewModel.CollectionVM.CurrentCollection.AllTags)
                {
                    Parent = referenceTag
                };
                ModalWindow modal = new(new TagEditViewModel(newTag, true));
                await modal.ShowDialog(App.MainWindow);
            }
        }

        private RelayCommand<Tag?>? _sortChildrenCommand;
        public RelayCommand<Tag?> SortChildrenCommand => _sortChildrenCommand ??= new(SortChildren, CanSortChildren);
        public void SortChildren(Tag? parentTag)
        {
            RecursiveSortChildren(parentTag);
            UpdateTagsAsTreeNodes();
        }
        public void RecursiveSortChildren(Tag? tag)
        {
            if (tag is null) return;
            tag.Children = new(tag.Children.OrderBy(t => t.Name));
            foreach (Tag child in tag.Children)
            {
                RecursiveSortChildren(child);
            }
        }
        
        public bool CanSortChildren(Tag? tag) => tag?.Children.Any() == true;

        private RelayCommand? _sortAllTagsCommand;
        public RelayCommand SortAllTagsCommand => _sortAllTagsCommand ??= new(SortAllTags);
        public void SortAllTags()
        {
            Tag t = new()
            {
                Children = new(MainViewModel.CollectionVM.CurrentCollection.RootTags)
            };
            RecursiveSortChildren(t);
            MainViewModel.CollectionVM.CurrentCollection.RootTags = t.Children.ToList();
            UpdateTagsAsTreeNodes();
        }

        private AsyncRelayCommand<Tag?>? _editTagCommand;
        public AsyncRelayCommand<Tag?> EditTagCommand => _editTagCommand ??= new(EditTag);
        public async Task EditTag(Tag? toEdit)
        {
            if (toEdit is null) return;
            ModalWindow modal = new(new TagEditViewModel(toEdit, false));
            await modal.ShowDialog(App.MainWindow);
        }

        private RelayCommand<Tag?>? _deleteTagCommand;
        public RelayCommand<Tag?> DeleteTagCommand => _deleteTagCommand ??= new(DeleteTag);

        public void DeleteTag(Tag? toDelete)
        {
            //tag to delete is context, because DeleteTag is called from context menu
            if (toDelete is null) return;
            MainViewModel.CollectionVM.CurrentCollection.DeleteTag(toDelete);
            _filterVM.RemoveFilter(new TagFilter(toDelete));

            //Go over all files and remove the tag from tag list
            foreach (var f in _codexCollection.AllCodices)
            {
                f.Tags.Remove(toDelete);
            }

            UpdateTagsAsTreeNodes();
        }
        #endregion
    }
}
