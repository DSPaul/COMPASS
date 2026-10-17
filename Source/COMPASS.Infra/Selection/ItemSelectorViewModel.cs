using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Infra.Objects;
namespace COMPASS.Infra.Selection
{
    public class ItemSelectorViewModel<T> : ObservableObject, IItemWrapper<T>
    {
        public ItemSelectorViewModel(T item, string? displayName = null)
        {
            Item = item;
            DisplayName = displayName ?? item?.ToString() ?? "";
        }

        public T Item { get; set; }
        public string DisplayName { get; }

        public bool Selected
        {
            get;
            set => SetProperty(ref field, value);
        } = true;
    }
}
