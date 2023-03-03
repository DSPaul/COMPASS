using System.Collections.ObjectModel;
using System.Linq;

namespace COMPASS.Models
{
    public class TreeViewNode : ObservableObject, IHasChilderen<TreeViewNode>
    {
        public TreeViewNode(Tag tag)
        {
            Tag = tag;
            Children = new(tag.Children.Select(childTag => new TreeViewNode(childTag)));
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
            //add childeren according to treeview
            Tag.Children = new(Children.Select(childnode => childnode.ToTag()));

            //set partentID for all the childeren
            foreach (Tag childtag in Tag.Children)
            {
                childtag.Parent = Tag;
            }

            return Tag;
        }
    }
}
