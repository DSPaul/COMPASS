using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Windows;
using GongSolutions.Wpf.DragDrop;
using System.Collections.ObjectModel;
using System.Linq;

namespace COMPASS.ViewModels
{
    public class TagsViewModel : ObservableObject, IDropTarget
    {
        public TagsViewModel(CollectionViewModel collectionVM)
        {
            _collectionVM = collectionVM;
            BuildTagTreeView();
        }

        private CollectionViewModel _collectionVM;

        //Tag for Context Menu
        public Tag ContextTag { get; set; }

        //TreeViewSource with hierarchy
        private ObservableCollection<TreeViewNode> _treeviewsource;
        public ObservableCollection<TreeViewNode> TreeViewSource
        {
            get => _treeviewsource;
            set => SetProperty(ref _treeviewsource, value);
        }

        public void BuildTagTreeView() => TreeViewSource = new(_collectionVM.CurrentCollection.RootTags.Select(tag => new TreeViewNode(tag)));

        //Tag Creation ViewModel
        private IEditViewModel _addTagViewModel;
        public IEditViewModel AddTagViewModel
        {
            get => _addTagViewModel;
            set => SetProperty(ref _addTagViewModel, value);
        }

        //Add Tag Btns

        private ActionCommand _addTagCommand;
        public ActionCommand AddTagCommand => _addTagCommand ??= new(AddTag);
        public void AddTag() => AddTagViewModel = new TagEditViewModel(null, true);


        private ActionCommand _addGroupCommand;
        public ActionCommand AddGroupCommand => _addGroupCommand ??= new(AddGroup);
        public void AddGroup()
        {
            Tag newTag = new(MainViewModel.CollectionVM.CurrentCollection.AllTags)
            {
                IsGroup = true,
            };
            AddTagViewModel = new TagEditViewModel(newTag, true);
        }

        private RelayCommand<object[]> _addTagFilterCommand;
        public RelayCommand<object[]> AddTagFilterCommand => _addTagFilterCommand ??= new(AddTagFilterHelper);
        public void AddTagFilterHelper(object[] par)
        {
            //needed because relaycommand only takes functions with one arg
            Tag tag = (Tag)par[0];
            bool include = (bool)par[1];
            _collectionVM.FilterVM.AddFilter(new(Filter.FilterType.Tag, tag), include);
        }


        #region Drag & Drop Tags Treeview
        //Drop on Treeview Behaviour
        void IDropTarget.DragOver(IDropInfo dropInfo) => DragDrop.DefaultDropHandler.DragOver(dropInfo);
        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            DragDrop.DefaultDropHandler.Drop(dropInfo);
            // Drag & Drop will modify the Collection of Treeviewnodes that the treeview is bound to
            // We need to convert that back to the collection of Tags so that the changes are saved
            var newRootTags = TreeViewSource.Select(node => node.ToTag());

            foreach (Tag t in newRootTags)
            {
                t.Parent = null;
            }

            // Cannot do TreeRoot = ExtractTagsFromTreeViewSource(TreeViewSource); because that changes ref of TreeRoot
            _collectionVM.CurrentCollection.RootTags.Clear();
            _collectionVM.CurrentCollection.RootTags.AddRange(newRootTags);
        }
        #endregion

        #region Tag Context Menu
        private ActionCommand _createChildCommand;
        public ActionCommand CreateChildCommand => _createChildCommand ??= new(CreateChildTag);
        private void CreateChildTag()
        {
            if (ContextTag != null)
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


        private ActionCommand _editTagCommand;
        public ActionCommand EditTagCommand => _editTagCommand ??= new(EditTag);
        public void EditTag()
        {
            if (ContextTag != null)
            {
                TagPropWindow tpw = new(new TagEditViewModel(ContextTag, false));
                tpw.ShowDialog();
                tpw.Topmost = true;
            }
        }

        private ActionCommand _deleteTagCommand;
        public ActionCommand DeleteTagCommand => _deleteTagCommand ??= new(DeleteTag);
        public void DeleteTag()
        {
            //tag to delete is context, because DeleteTag is called from context menu
            if (ContextTag == null) return;
            MainViewModel.CollectionVM.CurrentCollection.DeleteTag(ContextTag);
            _collectionVM.FilterVM.RemoveFilter(new(Filter.FilterType.Tag, ContextTag));

            //Go over all files and remove the tag from tag list
            foreach (var f in _collectionVM.CurrentCollection.AllCodices)
            {
                f.Tags.Remove(ContextTag);
            }

            BuildTagTreeView();
        }
        #endregion
    }
}
