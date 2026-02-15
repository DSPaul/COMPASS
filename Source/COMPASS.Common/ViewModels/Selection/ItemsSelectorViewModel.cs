using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace COMPASS.Common.ViewModels.Selection
{
    public class ItemsSelectorViewModel<T> : ObservableObject, IDisposable
    {
        public ItemsSelectorViewModel(IEnumerable<ItemSelectorViewModel<T>> items)
        {
            SelectableItems = new(items);
            foreach (var item in SelectableItems)
            {
                item.PropertyChanged += Item_PropertyChanged;
            }
        }
        
        public ItemsSelectorViewModel(IEnumerable<T> items, Func<T, string>? displayNameSelector = null)
            :this(items.Select(item => new ItemSelectorViewModel<T>(item, displayNameSelector?.Invoke(item))))
        { }

        public ObservableCollection<ItemSelectorViewModel<T>> SelectableItems { get; }

        public IEnumerable<T> GetSelectedItems() => SelectableItems.Where(x => x.Selected).Select(x => x.Item);
        
        public bool SelectAll
        {
            get => SelectableItems.All(x => x.Selected);
            set
            {
                foreach (var item in SelectableItems) item.Selected = value;
                OnPropertyChanged();
            }
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ItemSelectorViewModel<T>.Selected))
            {
                OnPropertyChanged(nameof(SelectAll));
            }
        }

        public void Dispose()
        {
            foreach (var item in SelectableItems)
            {
                item.PropertyChanged -= Item_PropertyChanged;
            }
        }
    }
}

