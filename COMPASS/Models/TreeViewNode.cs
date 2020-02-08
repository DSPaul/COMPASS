using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COMPASS.Models
{
    public class TreeViewNode : ObservableObject
{
        public TreeViewNode(Tag t)
        {
            Tag = t;
            Children = new ObservableCollection<TreeViewNode>();
        }


        private Tag _tag;
        public Tag Tag
        {
            get { return _tag; }
            set { SetProperty(ref _tag, value); }
        }

        private ObservableCollection<TreeViewNode> _children;
        public ObservableCollection<TreeViewNode> Children
        {
            get { return _children; }
            set { SetProperty(ref _children, value); }
        }

        private bool _selected = false;
        public bool Selected
        {
            get { return _selected; }
            set { SetProperty(ref _selected, value); }
        }

        private bool _expanded = true;
        public bool Expanded
        {
            get { return _expanded; }
            set { SetProperty(ref _expanded, value); }
        }

    }
}
