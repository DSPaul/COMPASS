using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Filters;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Infra.Tools.Logging;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.ViewModels.Main;

public class TabsViewModel(
    ILogger logger,
    CollectionManager collectionManager,
    CollectionTabVMFactory collectionTabVMFactory) : ViewModelBase
{
    private static TabsViewModel? _instance;

    /// <summary>
    /// Accessor for code that cannot receive constructor injection (static
    /// helpers, view code-behind). Services should inject TabsViewModel instead. TODO
    /// </summary>
    public static TabsViewModel GetInstance() => _instance ??= ServiceResolver.Resolve<TabsViewModel>();

    private readonly ILogger _logger = logger;
    private readonly CollectionManager _collectionManager = collectionManager;
    private readonly CollectionTabVMFactory _collectionTabVMFactory = collectionTabVMFactory;

    private int _tabIndex = 0;
    private Stack<TabState> _closedTabs = [];
    
    #region Properties
    
    public ObservableCollection<CollectionTabVM> Tabs { get; } = [];

    public int TabIndex
    {
        get => _tabIndex;
        set
        {
            int maxIndex = Math.Max(Tabs.Count - 1, 0);
            value = Math.Clamp(value, 0, maxIndex);
            SetProperty(ref _tabIndex, value);
            OnPropertyChanged(nameof(ActiveTab));
            TabChanged?.Invoke(this, ActiveTab);
        }
    }
        
    public CollectionTabVM? ActiveTab => Tabs.Any() ? Tabs[TabIndex] : null;

    #endregion

    #region Events
    
    public event EventHandler<CollectionTabVM>? TabCreated;
    public event EventHandler<CollectionTabVM>? TabClosed;
    public event EventHandler<CollectionTabVM?>? TabChanged;

    #endregion
    
    #region Commands
    
    private RelayCommand? _createTabCommand;
    public RelayCommand CreateTabCommand => _createTabCommand ??= new(() => CreateTab());
    
    private RelayCommand<CollectionTabVM>? _duplicateTabCommand;
    public RelayCommand<CollectionTabVM> DuplicateTabCommand => _duplicateTabCommand ??= new(DuplicateTab);
    
    private RelayCommand<CollectionTabVM>? _closeTabCommand;
    public RelayCommand<CollectionTabVM> CloseTabCommand => _closeTabCommand ??= new(CloseTab);
    #endregion

    #region Methods

    public CollectionTabVM CreateTab() => CreateTab(_collectionManager.GetOrCreateInitialCollectionVM());

    public CollectionTabVM CreateTab(CodexCollectionVM collectionVm, FiltersState? filtersState = null, CodexLayout? layout = null)
        => CreateTab(collectionVm.Load() ?? _collectionManager.GetOrCreateInitialCollectionVM(), filtersState, layout);

    private CollectionTabVM CreateTab(CollectionHandle collectionHandle, FiltersState? filtersState = null, CodexLayout? layout = null)
    {
        var tab = _collectionTabVMFactory.Create(collectionHandle, filtersState, layout);
        AddTab(tab);
        return tab;
    }

    private void CreateTab(TabState tabState)
    {
        if (_collectionManager.GetCollectionVM(tabState.CollectionId) is { } collectionVM)
        {
            CreateTab(collectionVM, tabState.FiltersState, tabState.Layout);
        }
        else
        {
            //Collection referenced in tab is no longer available
            //TODO should probably show a popup message here, but don't feel like making it all async atm
            _logger.Warn($"Failed to create tab for collection {tabState.CollectionId} because it is no longer available");
            CreateTab();
        }
    }

    public void AddTab(CollectionTabVM tab)
    {
        Tabs.Add(tab);
        TabIndex = Tabs.Count - 1;
        TabCreated?.Invoke(this, tab);
    }
    
    public void DuplicateTab(CollectionTabVM? tab)
    {
        if (tab == null)
        {
            return;
        }
        
        CreateTab(new TabState(tab));
    }
        
    public void CloseTab(CollectionTabVM? tab)
    {
        if (tab == null)
        {
            return;
        }
        
        //Decrement index if it was the rightmost tab
        if (tab == Tabs.Last())
        {
            TabIndex--;
        }
        
        _closedTabs.Push(new TabState(tab));
        
        Tabs.Remove(tab);
        TabClosed?.Invoke(this, tab);
        tab.Dispose();
        
        //if last remaining tab is closed, reopen a new one
        if (!Tabs.Any())
        {
            CreateTab();
        }
        
        TabChanged?.Invoke(this, ActiveTab);
    }

    public void ReopenTab()
    {
        if (!_closedTabs.Any()) return;
        var tabState = _closedTabs.Pop();
        CreateTab(tabState);
    }

    #endregion

    private class TabState(CodexCollectionVM collectionVM, FiltersState filtersState, CodexLayout layout)
    {
        public TabState(CollectionTabVM tabVM) : this(
            tabVM.CollectionVM, 
            tabVM.FiltersVM.GetFiltersState(), 
            tabVM.CurrentLayout.LayoutType) { }
        
        
        public string CollectionId { get; } = collectionVM.Identifier;
        public FiltersState FiltersState { get; } = filtersState;
        public CodexLayout Layout { get; } = layout;
    }
}