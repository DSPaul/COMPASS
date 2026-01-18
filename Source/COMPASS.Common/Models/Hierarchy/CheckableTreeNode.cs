using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Interfaces;

namespace COMPASS.Common.Models.Hierarchy
{
    public class CheckableTreeNode<T> : TreeNode<T>, IHasChildren<CheckableTreeNode<T>> where T : class, IHasChildren<T>
    {
        public CheckableTreeNode(T item, bool containerOnly = false, bool propagateChanges = false) : base (item)
        {
            ContainerOnly = containerOnly;
            PropagateChanges = propagateChanges;
            Children = new(item.Children.Select(child => new CheckableTreeNode<T>(child, containerOnly, propagateChanges)));

            // Children.CollectionChanged += (_, _) =>
            // {
            //     Update();
            //     foreach (var child in _children)
            //     {
            //         child.Parent = this;
            //     }
            // };
        }

        /// <summary>
        /// Indicates that the node only exists as a container for its children,
        /// cannot be checked on its own. Will be unchecked if all children are unchecked.
        /// </summary>
        public bool ContainerOnly { get; set; }

        /// <summary>
        /// Indicates that changes will propagate up and down
        /// A parent is automatically get checked if all children are checked
        /// All children will get checked if a parent is checked, ect. 
        /// </summary>
        public bool PropagateChanges { get; set; }
        

        private bool? _isChecked = false;
        public bool? IsChecked
        {
            get => _isChecked;
            set
            {
                if (SetProperty(ref _isChecked, value) && PropagateChanges)
                {
                    Parent?.Update();
                    PropagateDown(value);
                }
            }
        }

        /// <summary>
        /// Used to set the checked property without triggering updates up and down
        /// </summary>
        /// <param name="value"></param>
        private void InternalSetChecked(bool? value) => SetProperty(ref _isChecked, value, nameof(IsChecked));

        private RangeObservableCollection<CheckableTreeNode<T>> _children = [];
        public new RangeObservableCollection<CheckableTreeNode<T>> Children
        {
            get => _children;
            init
            {
                SetProperty(ref _children, value);
                Update();
                foreach (var child in _children)
                {
                    child.Parent = this;
                }
            }
        }

        public new RangeObservableCollection<CheckableTreeNode<T>> SelectableChildren => _children;

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
            Updated?.Invoke(this, IsChecked);
            Parent?.Update();
        }

        public event EventHandler<bool?>? Updated;

        public T? GetCheckedItems()
        {
            if (IsChecked == false) return default;

            Item.Children.ReplaceRange(
                Children.Where(child => child.IsChecked != false)
                        .Select(child => child.GetCheckedItems()!));

            return Item;
        }
    }

    public static class CheckableTreeNode
    {
        public static IEnumerable<T> GetCheckedItems<T>(IEnumerable<CheckableTreeNode<T>> items) 
            where T : class, IHasChildren<T>
        {
            return items.Where(item => item.IsChecked != false)
                .Select(item => item.GetCheckedItems()!);
        }

        public static IEnumerable<TModel> GetCheckedModels<TViewModel, TModel>(IEnumerable<CheckableTreeNode<TViewModel>> items)
            where TViewModel : ModelViewModelBase<TModel>, IHasChildren<TViewModel> where TModel : ObservableObject
        {
            return items.Where(item => item.IsChecked != false)
                .Select(item => item.GetCheckedItems()!.GetModel());
        }
    }
}
