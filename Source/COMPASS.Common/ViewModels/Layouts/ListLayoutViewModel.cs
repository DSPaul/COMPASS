using Avalonia.Collections;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Preferences;
using COMPASS.Common.Operations;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.ModelVMs;

namespace COMPASS.Common.ViewModels.Layouts
{
    public class ListLayoutViewModel : LayoutViewModel
    {
        public ListLayoutViewModel(
            ListLayoutPreferences preferences, 
            CodexInfoViewModelFactory codexInfoVmFactory,
            ImportFilesViewModelFactory importFilesVmFactory,
            CodexCollectionOperations collectionOperations,
            CollectionTabVM tabVM) : base(codexInfoVmFactory, importFilesVmFactory, collectionOperations, tabVM)
        {
            Preferences = preferences;
            SubscribeToCollectionChangedEvent();
        }
        
        public DataGridCollectionView? CodexCollectionView { get; set => SetProperty(ref field, value); }

        protected override void OnCollectionChanging(object? sender, EventArgs? e)
        {
            FiltersVM.CodicesUpdated -= OnFilteredCodicesChanged;
            base.OnCollectionChanging(sender, e);
        }

        protected override void OnCollectionChanged(object? sender, EventArgs? e)
        {
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

    public class ListLayoutViewModelFactory(
        IPreferencesService preferencesService,
        CodexInfoViewModelFactory codexInfoVmFactory,
        ImportFilesViewModelFactory importFilesVmFactory,
        CodexCollectionOperations collectionOperations) : LayoutViewModelFactoryBase
    {
        public override LayoutViewModel Create(CollectionTabVM tabVm) => new ListLayoutViewModel(
            preferencesService.Preferences.ListLayoutPreferences, codexInfoVmFactory,
            importFilesVmFactory, collectionOperations, tabVm);
    }
}
