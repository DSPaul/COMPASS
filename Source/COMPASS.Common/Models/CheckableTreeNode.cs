using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace COMPASS.Common.Models
{
    public class CheckableTreeNode<T> : ObservableObject, IHasChildren<CheckableTreeNode<T>> where T : class, IHasChildren<T>
    {
        public CheckableTreeNode(T item, bool containerOnly)
        {
            _item = item;
            ContainerOnly = containerOnly;
            Children = new(item.Children.Select(child => new CheckableTreeNode<T>(child, containerOnly)));

            Children.CollectionChanged += (_, _) =>
            {
                Update();
                foreach (var child in _children)
                {
                    child.Parent = this;
                }
            };
        }

        private T _item;
        public T Item
        {
            get => _item;
            set => SetProperty(ref _item, value);
        }

        /// <summary>
        /// Indicates that the node only exists as a container for its children,
        /// cannot be checked on it's own. Will be unchecked if all children are unchecked.
        /// </summary>
        public bool ContainerOnly { get; set; }

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
                    PropagateDown(value);
                }
            }
        }

        /// <summary>
        /// Used to set the checked property without triggering updates up and down
        /// </summary>
        /// <param name="value"></param>
        private void InternalSetChecked(bool? value) => SetProperty(ref _isChecked, value, nameof(IsChecked));

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

        public CheckableTreeNode<T>? Parent { get; set; }

        public void PropagateDown(bool? isChecked)
        {
            if (isChecked != null)
            {
                InternalSetChecked(isChecked);
                foreach (var child in Children)
                {
                    child.PropagateDown(isChecked);
                }
            }
        }

        private void Update()
        {
            bool? newValue = _isChecked;
            if (Children.All(child => child.IsChecked == true))
            {
                newValue = true;
            }
            else if (Children.All(child => child.IsChecked == false))
            {
                if (ContainerOnly)
                {
                    newValue = false;
                }
                else newValue ??= true; //if it was null (partial check), become full check
            }
            else
            {
                newValue = null;
            }

            InternalSetChecked(newValue);
            Updated?.Invoke(IsChecked);
            Parent?.Update();
        }

        public event Action<bool?>? Updated;

        private bool _expanded = true;
        public bool Expanded
        {
            get => _expanded;
            set => SetProperty(ref _expanded, value);
        }

        public T? GetCheckedItems()
        {
            if (IsChecked == false) return default;

            Item.Children = new(Children.Where(child => child.IsChecked != false)
                                        .Select(child => child.GetCheckedItems()!));

            return Item;
        }

        public static IEnumerable<T> GetCheckedItems(IEnumerable<CheckableTreeNode<T>> items) =>
            items.Where(item => item.IsChecked != false)
                 .Select(item => item.GetCheckedItems()!);
    }
}
