using Avalonia.Threading;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Preferences;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Infra.Avalonia.ExtensionMethods;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace COMPASS.Common.ViewModels.Layouts
{
    internal class HomeLayoutViewModel : LayoutViewModel
    {
        public HomeLayoutViewModel(CollectionTabVM tabVM) : base(tabVM)
        {
            Preferences = PreferencesService.Preferences.HomeLayoutPreferences;
            tabVM.CollectionVM.CodexPropertyChanged += OnCodexPropertyChanged;
            tabVM.FiltersVM.CodicesUpdated += OnFilteredCodicesChanged;
        }

        public HomeLayoutPreferences Preferences { get; set; }
        
        public override CodexLayout LayoutType => CodexLayout.Home;

        public int ItemsShown
        {
            get => Math.Min(field, FiltersVM.FilteredCodices?.Count ?? 0);
        } = 15;

        public ObservableCollection<CodexViewModel> Favorites => new(FiltersVM.FilteredCodices.Where(c => c.Favorite));
        public List<CodexViewModel> RecentCodices => FiltersVM.FilteredCodices.OrderByDescending(c => c.LastOpened).ToList().GetRange(0, ItemsShown);
        public List<CodexViewModel> MostOpenedCodices => FiltersVM.FilteredCodices.OrderByDescending(c => c.OpenedCount).ToList().GetRange(0, ItemsShown);
        public List<CodexViewModel> RecentlyAddedCodices => FiltersVM.FilteredCodices.OrderByDescending(c => c.DateAdded).ToList().GetRange(0, ItemsShown);

        protected override void OnCollectionChanging(object? sender, EventArgs? e)
        {
            _tabViewModel.CollectionVM.CodexPropertyChanged -= OnCodexPropertyChanged;
            _tabViewModel.FiltersVM.CodicesUpdated -= OnFilteredCodicesChanged;
            base.OnCollectionChanging(sender, e);
        }
        protected override void OnCollectionChanged(object? sender, EventArgs? e)
        {
            _tabViewModel.CollectionVM.CodexPropertyChanged += OnCodexPropertyChanged;
            _tabViewModel.FiltersVM.CodicesUpdated += OnFilteredCodicesChanged;
            base.OnCollectionChanged(sender, e);
        }

        private void OnFilteredCodicesChanged(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.PostIfNeeded(() =>
            {
                OnPropertyChanged(nameof(Favorites));
                OnPropertyChanged(nameof(RecentCodices));
                OnPropertyChanged(nameof(MostOpenedCodices));
                OnPropertyChanged(nameof(RecentlyAddedCodices));
            });
        }

        private void OnCodexPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Dispatcher.UIThread.PostIfNeeded(() =>
            {
                if (e.PropertyName == nameof(CodexViewModel.Favorite))
                {
                    OnPropertyChanged(nameof(Favorites));
                }
                else if (e.PropertyName == nameof(CodexViewModel.LastOpened))
                {
                    OnPropertyChanged(nameof(RecentCodices));
                }
                else if (e.PropertyName == nameof(CodexViewModel.OpenedCount))
                {
                    OnPropertyChanged(nameof(MostOpenedCodices));
                }
                else if (e.PropertyName == nameof(CodexViewModel.DateAdded))
                {
                    OnPropertyChanged(nameof(RecentlyAddedCodices));
                }
            });
        }
    }
}
