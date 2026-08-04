using System.Collections.Specialized;
using Avalonia.Collections;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Preferences;
using COMPASS.Common.ViewModels.Main;

namespace COMPASS.Common.ViewModels.Layouts
{
    public class ListLayoutViewModel : LayoutViewModel
    {
        public ListLayoutViewModel(CollectionTabVM tabVM) : base(tabVM)
        {
            Preferences = PreferencesService.Preferences.ListLayoutPreferences;
            SubscribeToCollectionChangedEvent();
        }
        
        public DataGridCollectionView? CodexCollectionView { get; set => SetProperty(ref field, value); }

        protected override void OnCollectionChanged(object? sender, EventArgs? e)
        {
            FiltersVM.CodicesUpdated -= OnFilteredCodicesChanged;
            base.OnCollectionChanged(sender, e);
            SubscribeToCollectionChangedEvent();
        }

        private void SubscribeToCollectionChangedEvent()
        {
            CodexCollectionView = new DataGridCollectionView(FiltersVM.FilteredCodices, true, false);
            FiltersVM.CodicesUpdated += OnFilteredCodicesChanged;
            OnFilteredCodicesChanged(null, EventArgs.Empty);
        }

        private void OnFilteredCodicesChanged(object? sender, EventArgs e)
        {
            if (CodexCollectionView == null)
            {
                return;
            }
            
            //Sync up sort description
            CodexCollectionView.SortDescriptions.Clear();
            var sortDescription = DataGridSortDescription.FromPath(FiltersVM.SortProperty, FiltersVM.SortDirection);
            CodexCollectionView.SortDescriptions.Add(sortDescription);
            
            CodexCollectionView?.Refresh();
        }

        public ListLayoutPreferences Preferences { get; }
        
        public override CodexLayout LayoutType => CodexLayout.List;
    }
}
