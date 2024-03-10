using System.Collections.ObjectModel;
using System.Linq;

namespace COMPASS.Models
{
    public class TreeViewNode : ObservableObject, IHasChildren<TreeViewNode>
    {
        public TreeViewNode(Tag tag)
        {
            _tag = tag;
            Children = new(tag.Children.Select(childTag => new TreeViewNode(childTag)));
            Expanded = tag.IsGroup;
        }

        private Tag _tag;
        public Tag Tag
        {
            get => _tag;
            set => SetProperty(ref _tag, value);
        }

        private ObservableCollection<TreeViewNode> _children = new();
        public ObservableCollection<TreeViewNode> Children
        {
            get => _children;
            set => SetProperty(ref _children, value);
        }

        private bool _selected = false;
        public bool Selected
        {
            get => _selected;
            set => SetProperty(ref _selected, value);
        }

        private bool _expanded = true;
        public bool Expanded
        {
            get => _expanded;
            set => SetProperty(ref _expanded, value);
        }

        public Tag ToTag()
        {
            //add children according to treeview
            Tag.Children = new(Children.Select(childNode => childNode.ToTag()));

            //set parentID for all the children
            foreach (Tag childTag in Tag.Children)
            {
                childTag.Parent = Tag;
            }

            return Tag;
        }
    }
}
