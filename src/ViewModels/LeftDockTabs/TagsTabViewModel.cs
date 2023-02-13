using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Windows;
using GongSolutions.Wpf.DragDrop;
using System.Collections.ObjectModel;
using System.Linq;

namespace COMPASS.ViewModels
{
    public class TagsTabViewModel : ViewModelBase, IDropTarget
    {
        public TagsTabViewModel()
        {
            BuildTagTreeView();

            AddTagCommand = new(AddTag);
            AddGroupCommand = new(AddGroup);
            EditTagCommand = new(EditTag);
            DeleteTagCommand = new(DeleteTag);
        }

        //Tag for Context Menu
        public Tag ContextTag { get; set; }

        //TreeViewSource with hierarchy
        private ObservableCollection<TreeViewNode> _treeviewsource;
        public ObservableCollection<TreeViewNode> TreeViewSource
        {
            get => _treeviewsource;
            set => SetProperty(ref _treeviewsource, value);
        }

        public void BuildTagTreeView() => TreeViewSource = new(MVM.CurrentCollection.RootTags.Select(tag => new TreeViewNode(tag)));

        //Tag Creation ViewModel
        private IEditViewModel _addTagViewModel;
        public IEditViewModel AddTagViewModel
        {
            get => _addTagViewModel;
            set => SetProperty(ref _addTagViewModel, value);
        }

        //Add Tag Btns
        public ActionCommand AddTagCommand { get; private set; }
        public ActionCommand AddGroupCommand { get; private set; }
        public void AddTag() => AddTagViewModel = new TagEditViewModel(null, false);

        public void AddGroup() => AddTagViewModel = new TagEditViewModel(null, true);

        private RelayCommand<object[]> _addTagFilterCommand;
        public RelayCommand<object[]> AddTagFilterCommand => _addTagFilterCommand ??= new(AddTagFilterHelper);
        public void AddTagFilterHelper(object[] par)
        {
            Tag tag = (Tag)par[0];
            bool include = (bool)par[1];
            MVM.FilterVM.AddFilter(new(Filter.FilterType.Tag, tag), include); //needed because relaycommand only takes functions with one arg
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
            MVM.CurrentCollection.RootTags.Clear();
            MVM.CurrentCollection.RootTags.AddRange(newRootTags);
        }
        #endregion

        #region Tag Context Menu
        public ActionCommand EditTagCommand { get; init; }
        public void EditTag()
        {
            if (ContextTag != null)
            {
                TagPropWindow tpw = new(new TagEditViewModel(ContextTag));
                tpw.ShowDialog();
                tpw.Topmost = true;
            }
        }

        public ActionCommand DeleteTagCommand { get; init; }
        public void DeleteTag()
        {
            //tag to delete is context, because DeleteTag is called from context menu
            if (ContextTag == null) return;
            MVM.CurrentCollection.DeleteTag(ContextTag);
            MVM.FilterVM.RemoveFilter(new(Filter.FilterType.Tag, ContextTag));

            //Go over all files and remove the tag from tag list
            foreach (var f in MVM.CurrentCollection.AllCodices)
            {
                f.Tags.Remove(ContextTag);
            }

            MVM.Refresh();
        }
        #endregion
    }
}
