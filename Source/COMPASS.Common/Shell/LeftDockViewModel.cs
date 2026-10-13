using COMPASS.Common.Features.ImportExport;
using COMPASS.Common.Features.Logging;
using COMPASS.Common.Features.Settings;
using COMPASS.Infra.DependencyInjection;
using COMPASS.Infra.Preferences;
using COMPASS.Infra.Avalonia.Validation;
using COMPASS.Common.Features.Tabs;

namespace COMPASS.Common.Shell;

public class LeftDockViewModel : ValidatableViewModelBase
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
        => new(tabsViewModel, preferencesService.GetPreferences<UIState>(), addCodexPanelVMFactory);
}
