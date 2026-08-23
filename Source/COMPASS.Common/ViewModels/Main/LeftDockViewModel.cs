using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models.Preferences;
using COMPASS.Common.ViewModels.SidePanels;

namespace COMPASS.Common.ViewModels.Main;

public class LeftDockViewModel : ViewModelBase
{
    private readonly TabsViewModel _tabsVM;
    private readonly UIState _uiState;

    public LeftDockViewModel(
        TabsViewModel tabsViewModel,
        UIState uiState,
        AddCodexPanelVMFactory addCodexPanelVMFactory)
    {
        _tabsVM = tabsViewModel;
        _uiState = uiState;

        AddCodexPanelVM = addCodexPanelVMFactory.Create();
        LogsVM = new();
    }

    public AddCodexPanelVM AddCodexPanelVM { get; }
    public LogsVM LogsVM { get; }

    public TabsViewModel TabsVM
    {
        get => _tabsVM;
        init => SetProperty(ref _tabsVM, value);
    }

    public int SelectedTab
    {
        get => _uiState.StartupTab;
        set
        {
            _uiState.StartupTab = value;
            OnPropertyChanged();
            if (value >= 0) Collapsed = false;
        }
    }

    private bool _collapsed = false;
    public bool Collapsed
    {
        get => _collapsed;
        set
        {
            SetProperty(ref _collapsed, value);
            if (value) SelectedTab = -1;
        }
    }
}

[Factory]
public class LeftDockViewModelFactory(
    IPreferencesService preferencesService,
    AddCodexPanelVMFactory addCodexPanelVMFactory)
{
    public LeftDockViewModel Create(TabsViewModel tabsViewModel) 
        => new(tabsViewModel, preferencesService.Preferences.UIState, addCodexPanelVMFactory);
}