using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;

namespace COMPASS.Common.Models
{
    public class TreeNode : ObservableObject, IHasChildren<TreeNode>
    {
        public TreeNode(Tag tag)
        {
            _tag = tag;
            Children = new(tag.Children.Select(childTag => new TreeNode(childTag)));
            Expanded = tag.IsGroup;
        }

        private Tag _tag;
        public Tag Tag
        {
            get => _tag;
            set => SetProperty(ref _tag, value);
        }

        private ObservableCollection<TreeNode> _children = [];
        public ObservableCollection<TreeNode> Children
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
