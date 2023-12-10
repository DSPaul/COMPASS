using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace COMPASS.Models
{
    public class CheckableTreeNode<T> : ObservableObject, IHasChildren<CheckableTreeNode<T>> where T : class, IHasChildren<T>
    {

        public CheckableTreeNode()
        {
            Children.CollectionChanged += (_, _) =>
            {
                Update();
                foreach (var child in _children)
                {
                    child.Parent = this;
                }
            };
        }

        public CheckableTreeNode(T item) : this()
        {
            Item = item;
            Children = new(item.Children.Select(child => new CheckableTreeNode<T>(child)));
        }

        private T _item;
        public T Item
        {
            get => _item;
            set => SetProperty(ref _item, value);
        }

        private bool? _isChecked = false;
        public bool? IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    SetProperty(ref _isChecked, value);
                    //Propagate changes upward
                    Parent?.Update();
                    //propagate changes downwards
                    if (value != null)
                    {
                        foreach (var child in Children)
                        {
                            child.IsChecked = value;
                        }
                    }
                }
            }
        }

        private ObservableCollection<CheckableTreeNode<T>> _children = new();
        public ObservableCollection<CheckableTreeNode<T>> Children
        {
            get => _children;
            set
            {
                SetProperty(ref _children, value);
                Update();
                foreach (var child in _children)
                {
                    child.Parent = this;
                }
            }
        }

        public CheckableTreeNode<T> Parent { get; set; }

        private void Update()
        {
            if (Children.All(child => child.IsChecked == true))
            {
                IsChecked = true;
            }
            else if (Children.All(child => child.IsChecked == false))
            {
                IsChecked = false;
            }
            else
            {
                IsChecked = null;
            }
            if (Updated != null)
            {
                Updated(IsChecked);
            }
        }

        public event Action<bool?> Updated;

        private bool _expanded = true;
        public bool Expanded
        {
            get => _expanded;
            set => SetProperty(ref _expanded, value);
        }

        public T GetCheckedItems()
        {
            if (IsChecked == false) return default;
            Item.Children = new(Children.Where(child => child.IsChecked != false)
                                        .Select(child => child.GetCheckedItems()));
            return Item;
        }

        public static IEnumerable<T> GetCheckedItems(IEnumerable<CheckableTreeNode<T>> items) =>
            items.Where(child => child.IsChecked != false)
                 .Select(child => child.GetCheckedItems());
    }
}
