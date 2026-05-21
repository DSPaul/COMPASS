using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.ViewModels.SidePanels;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.ViewModels.Main
{
    public class LeftDockViewModel : ViewModelBase, IDealsWithTabControl
    {
        public LeftDockViewModel(TabsViewModel tabsViewModel)
        {
            _tabsVM = tabsViewModel;
            _preferencesService = ServiceResolver.Resolve<IPreferencesService>();

            AddCodexPanelVM = new();
            LogsVM = new();
        }

        private TabsViewModel _tabsVM;
        private IPreferencesService _preferencesService;

        public AddCodexPanelVM AddCodexPanelVM { get; }
        public LogsVM LogsVM { get; }

        public TabsViewModel TabsVM
        {
            get => _tabsVM;
            init => SetProperty(ref _tabsVM, value);
        }

        public int SelectedTab
        {
            get => _preferencesService.Preferences.UIState.StartupTab;
            set
            {
                PrevSelectedTab = _preferencesService.Preferences.UIState.StartupTab;
                _preferencesService.Preferences.UIState.StartupTab = value;
                OnPropertyChanged();
                if (value > 0) Collapsed = false;
            }
        }

        public int PrevSelectedTab { get; set; }

        private bool _collapsed = false;
        public bool Collapsed
        {
            get => _collapsed;
            set
            {
                SetProperty(ref _collapsed, value);
                if (value) SelectedTab = 0;
            }
        }
    }
}
