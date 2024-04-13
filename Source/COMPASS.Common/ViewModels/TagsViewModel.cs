using COMPASS.Common.Commands;
using COMPASS.Common.Models;
using COMPASS.Common.Services;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.Views.Windows;
using GongSolutions.Wpf.DragDrop;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace COMPASS.Common.ViewModels
{
    public class TagsViewModel : ViewModelBase, IDropTarget, IDealsWithTabControl
    {
        public TagsViewModel(CodexCollection codexCollection, FilterViewModel filterVM)
        {
            _codexCollection = codexCollection;
            _filterVM = filterVM;
            BuildTagTreeView();
        }

        private readonly CodexCollection _codexCollection;
        private readonly FilterViewModel _filterVM;

        //Tag for Context Menu
        public Tag? ContextTag { get; set; }

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
                SetProperty(ref _selectedTab, value);
            }
        }

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

        //TreeViewSource with hierarchy
        private ObservableCollection<TreeViewNode> _treeViewSource = new();
        public ObservableCollection<TreeViewNode> TreeViewSource
        {
            get => _treeViewSource;
            set => SetProperty(ref _treeViewSource, value);
        }

        public void BuildTagTreeView()
        {
            List<TreeViewNode> newTreeViewSource = _codexCollection.RootTags.Select(tag => new TreeViewNode(tag)).ToList();

            // transfer expanded property
            if (TreeViewSource.Any())
            {
                var oldNodes = TreeViewSource.Flatten().ToList();
                foreach (TreeViewNode newNode in newTreeViewSource.Flatten())
                {
                    newNode.Expanded = oldNodes.Find(n => n.Tag == newNode.Tag)?.Expanded ?? newNode.Expanded;
                }
            }
            TreeViewSource = new(newTreeViewSource);
        }

        //Tag Creation ViewModel
        private IEditViewModel? _addTagViewModel;
        public IEditViewModel? AddTagViewModel
        {
            get => _addTagViewModel;
            set => SetProperty(ref _addTagViewModel, value);
        }

        //Group Creation ViewModel
        private IEditViewModel? _addGroupViewModel;
        public IEditViewModel? AddGroupViewModel
        {
            get => _addGroupViewModel;
            set => SetProperty(ref _addGroupViewModel, value);
        }

        //Add Tag Buttons

        private ActionCommand? _addTagCommand;
        public ActionCommand AddTagCommand => _addTagCommand ??= new(AddTag);
        public void AddTag() => AddTagViewModel = new TagEditViewModel(null, true);


        private ActionCommand? _addGroupCommand;
        public ActionCommand AddGroupCommand => _addGroupCommand ??= new(AddGroup);
        public void AddGroup()
        {
            Tag newTag = new()
            {
                IsGroup = true,
            };
            AddGroupViewModel = new TagEditViewModel(newTag, true);
        }

        private RelayCommand<object[]>? _addTagFilterCommand;
        public RelayCommand<object[]> AddTagFilterCommand => _addTagFilterCommand ??= new(AddTagFilterHelper);
        public void AddTagFilterHelper(object[]? par)
        {
            if (par == null) return;
            //needed because relay command only takes functions with one arg
            Tag tag = (Tag)par[0];
            bool include = (bool)par[1];
            _filterVM.AddFilter(new(Filter.FilterType.Tag, tag), include);
        }

        private ActionCommand? _importTagsFromOtherCollectionsCommand;
        public ActionCommand ImportTagsFromOtherCollectionsCommand => _importTagsFromOtherCollectionsCommand ??= new(ImportTagsFromOtherCollections);
        public void ImportTagsFromOtherCollections()
        {
            var importVM = new ImportTagsViewModel(MainViewModel.CollectionVM.AllCodexCollections.ToList());
            var w = new ImportTagsWindow(importVM);
            w.Show();
        }

        private ActionCommand? _importTagsFromSatchelCommand;
        public ActionCommand ImportTagsFromSatchelCommand => _importTagsFromSatchelCommand ??= new(async () => await ImportTagsFromSatchel());
        public async Task ImportTagsFromSatchel()
        {
            var collectionToImport = await IOService.OpenSatchel();

            if (collectionToImport == null)
            {
                Logger.Warn("Failed to open file");
                return;
            }

            var importVM = new ImportTagsViewModel(collectionToImport);

            if (!importVM.TagsSelectorVM.HasTags)
            {
                messageDialog.Show($"{collectionToImport.DirectoryName[2..]} does not contain tags", "No Tags found", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            var w = new ImportTagsWindow(importVM);
            w.Show();
        }

        private ActionCommand? _exportTagsCommand;
        public ActionCommand ExportTagsCommand => _exportTagsCommand ??= new(ExportTags, _codexCollection.RootTags.Any);
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

        #region Drag & Drop Tags Treeview
        //Drop on Treeview Behaviour
        void IDropTarget.DragOver(IDropInfo dropInfo) => DragDrop.DefaultDropHandler.DragOver(dropInfo);
        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            DragDrop.DefaultDropHandler.Drop(dropInfo);
            // Drag & Drop will modify the Collection of Treeview nodes that the treeview is bound to
            // We need to convert that back to the collection of Tags so that the changes are saved
            var newRootTags = TreeViewSource.Select(node => node.ToTag()).ToList();

            foreach (Tag t in newRootTags)
            {
                t.Parent = null;
            }

            // Cannot do TreeRoot = ExtractTagsFromTreeViewSource(TreeViewSource); because that changes ref of TreeRoot
            _codexCollection.RootTags.Clear();
            _codexCollection.RootTags.AddRange(newRootTags);
        }
        #endregion

        #region Tag Context Menu
        private ActionCommand? _createChildCommand;
        public ActionCommand CreateChildCommand => _createChildCommand ??= new(CreateChildTag);
        private void CreateChildTag()
        {
            if (ContextTag is not null)
            {
                Tag newTag = new(MainViewModel.CollectionVM.CurrentCollection.AllTags)
                {
                    Parent = ContextTag,
                    SerializableBackgroundColor = null,
                };
                TagPropWindow tpw = new(new TagEditViewModel(newTag, true));
                tpw.ShowDialog();
                tpw.Topmost = true;
            }
        }

        private ActionCommand? _sortChildrenCommand;
        public ActionCommand SortChildrenCommand => _sortChildrenCommand ??= new(SortChildren, CanSortChildren);
        public void SortChildren()
        {
            SortChildren(ContextTag);
            BuildTagTreeView();
        }
        public void SortChildren(Tag? tag)
        {
            if (tag is null) return;
            tag.Children = new(tag.Children.OrderBy(t => t.Content));
            foreach (Tag child in tag.Children)
            {
                SortChildren(child);
            }
        }
        public bool CanSortChildren() => CanSortChildren(ContextTag);
        public bool CanSortChildren(Tag? tag) => tag?.Children.Any() == true;

        private ActionCommand? _sortAllTagsCommand;
        public ActionCommand SortAllTagsCommand => _sortAllTagsCommand ??= new(SortAllTags);
        public void SortAllTags()
        {
            Tag t = new()
            {
                Children = new(MainViewModel.CollectionVM.CurrentCollection.RootTags)
            };
            SortChildren(t);
            MainViewModel.CollectionVM.CurrentCollection.RootTags = t.Children.ToList();
            BuildTagTreeView();
        }

        private ActionCommand? _editTagCommand;
        public ActionCommand EditTagCommand => _editTagCommand ??= new(EditTag);
        public void EditTag()
        {
            if (ContextTag is not null)
            {
                TagPropWindow tpw = new(new TagEditViewModel(ContextTag, false));
                tpw.ShowDialog();
                tpw.Topmost = true;
            }
        }

        private ActionCommand? _deleteTagCommand;
        public ActionCommand DeleteTagCommand => _deleteTagCommand ??= new(DeleteTag);
        public void DeleteTag()
        {
            //tag to delete is context, because DeleteTag is called from context menu
            if (ContextTag is null) return;
            MainViewModel.CollectionVM.CurrentCollection.DeleteTag(ContextTag);
            _filterVM.RemoveFilter(new(Filter.FilterType.Tag, ContextTag));

            //Go over all files and remove the tag from tag list
            foreach (var f in _codexCollection.AllCodices)
            {
                f.Tags.Remove(ContextTag);
            }

            BuildTagTreeView();
        }
        #endregion
    }
}
