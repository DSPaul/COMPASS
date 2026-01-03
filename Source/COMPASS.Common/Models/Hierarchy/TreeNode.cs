using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Interfaces;

namespace COMPASS.Common.Models.Hierarchy
{
    public class TreeNode<T> : ObservableObject, IHasChildren<TreeNode<T>>, IItemWrapper<T>, IExpandable where T : class, IHasChildren<T>
    {
        public TreeNode (T item)
        {
            _item = item;
            Children = new(item.Children.Select(child => new TreeNode<T>(child)));

            //TODO, this logic should not be here
            if (item is Tag tag)
            {
                Expanded = tag.IsGroup;
            }
        }
        
        private RangeObservableCollection<TreeNode<T>> _children = [];
        public RangeObservableCollection<TreeNode<T>> Children
        {
            get => _children;
            set => SetProperty(ref _children, value);
        }

        private bool _expanded = true;
        public bool Expanded
        {
            get => _expanded;
            set => SetProperty(ref _expanded, value);
        }

        private T _item;
        public T Item
        {
            get => _item;
            set => SetProperty(ref _item, value);
        }
    }
}
