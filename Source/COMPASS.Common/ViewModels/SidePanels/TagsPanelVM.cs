using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Filters;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.Views.Windows;

namespace COMPASS.Common.ViewModels.SidePanels
{
    public class TagsPanelVM : ViewModelBase, IDealsWithTabControl
    {
        public TagsPanelVM(CodexCollection codexCollection, FilterViewModel filterVM)
        {
            _codexCollection = codexCollection;
            _filterVM = filterVM;
            BuildTagTreeView();
        }

        private readonly CodexCollection _codexCollection;
        private readonly FilterViewModel _filterVM;

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
        private ObservableCollection<TreeViewNode> _treeViewSource = [];
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

        private RelayCommand<Tag>? _addTagFilterCommand;
        public RelayCommand<Tag> AddTagFilterCommand => _addTagFilterCommand ??= new(AddTagFilterHelper);
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
            var collectionToImport = await IOService.OpenSatchel();

            if (collectionToImport == null)
            {
                Logger.Warn("Failed to open file");
                return;
            }

            var importVM = new ImportTagsViewModel(collectionToImport);

            if (!importVM.TagsSelectorVM.HasTags)
            {
                Notification noTagsFound = new("No Tags found", $"{collectionToImport.DirectoryName[2..]} does not contain tags");
                await App.Container.ResolveKeyed<INotificationService>(NotificationDisplayType.Windowed).Show(noTagsFound);
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

        #region Drag & Drop Tags Treeview
        //Drop on Treeview Behaviour
        //TODO: used to have to call the default implemenation of drag and drop here, not sure 
        void OnDrop(object sender, DragEventArgs e)
        {
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
                TagPropWindow tpw = new(new TagEditViewModel(newTag, true))
                {
                    Topmost = true
                };
                await tpw.ShowDialog(App.MainWindow);
            }
        }

        private RelayCommand<Tag?>? _sortChildrenCommand;
        public RelayCommand<Tag?> SortChildrenCommand => _sortChildrenCommand ??= new(SortChildren, CanSortChildren);
        public void SortChildren(Tag? parentTag)
        {
            RecursiveSortChildren(parentTag);
            BuildTagTreeView();
        }
        public void RecursiveSortChildren(Tag? tag)
        {
            if (tag is null) return;
            tag.Children = new(tag.Children.OrderBy(t => t.Content));
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
            BuildTagTreeView();
        }

        private AsyncRelayCommand<Tag?>? _editTagCommand;
        public AsyncRelayCommand<Tag?> EditTagCommand => _editTagCommand ??= new(EditTag);
        public async Task EditTag(Tag? toEdit)
        {
            if (toEdit is not null)
            {
                TagPropWindow tpw = new(new TagEditViewModel(toEdit, false))
                {
                    Topmost = true
                };
                await tpw.ShowDialog(App.MainWindow);
            }
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

            BuildTagTreeView();
        }
        #endregion
    }
}
