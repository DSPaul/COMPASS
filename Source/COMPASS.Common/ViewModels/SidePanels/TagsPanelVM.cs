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
using COMPASS.Common.ViewModels.Modals;
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
        public int SelectedTab
        {
            get;
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

                PrevSelectedTab = field;
                SetProperty(ref field, value);
            }
        } = 0;

        public int PrevSelectedTab { get ; set ; }

        public bool Collapsed
        {
            get;
            set
            {
                SetProperty(ref field, value);
                if (value) SelectedTab = 0;
            }
        } = false;

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
        public void AddTag() => AddTagViewModel = new TagEditViewModel(new Tag(), _codexCollectionVm, true);


        public RelayCommand AddGroupCommand => field ??= new(AddGroup);
        public void AddGroup()
        {
            Tag newTag = new()
            {
                IsGroup = true,
            };
            AddGroupViewModel = new TagEditViewModel(newTag, _codexCollectionVm, true);
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
            var importVM = new ImportTagsViewModel(CollectionManager.CollectionNames, ActiveCollection.Name);
            await WindowManager.OpenModal(importVM);
        }

        public AsyncRelayCommand ImportTagsFromSatchelCommand => field ??= new(ImportTagsFromSatchel);
        public async Task ImportTagsFromSatchel()
        {
            var importService = ServiceResolver.Resolve<IImportExportService>();
            var importCollection = await importService.OpenSatchel();

            if (importCollection == null)
            {
                Logger.Warn("Failed to open file");
                return;
            }

            if (!importCollection.RootTags.Any())
            {
                Notification noTagsFound = new("No Tags found", $"{importCollection.Name[2..]} does not contain tags");
                await ServiceResolver.Resolve<INotificationService>().ShowDialog(noTagsFound);
                return;
            }
            
            using CodexCollectionVM toImportVm = new(importCollection, StorageStrategy.Xml);
            var tagImportVM = new ImportTagsViewModel(toImportVm, ActiveCollection.Name);

            var w = new ModalWindow(tagImportVM);
            await w.ShowDialog(WindowManager.ActiveWindow);

            //Delete satchel when done
            toImportVm.DeleteCollection();
        }

        public AsyncRelayCommand ExportTagsCommand => field ??= new(ExportTags);
        public async Task ExportTags()
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

            var w = new ModalWindow(vm);
            await w.ShowDialog(WindowManager.ActiveWindow);
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
            tag.Children.ReplaceRange(node.Children.Select(ToTag));

            //set parentID for all the children
            foreach (Tag childTag in tag.Children)
            {
                childTag.Parent = tag;
            }

            return tag;
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
                var vm = new TagEditViewModel(newTag, _codexCollectionVm, true);
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
            var vm = new TagEditViewModel(toEdit.GetModel(), _codexCollectionVm, false);
            await WindowManager.OpenModal(vm);
        }

        public RelayCommand<TagViewModel?> DeleteTagCommand => field ??= new(DeleteTag);

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
