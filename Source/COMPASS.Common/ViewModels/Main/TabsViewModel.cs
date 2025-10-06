using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Models;
using COMPASS.Common.Services.StateManagers;
using iText.Svg;

namespace COMPASS.Common.ViewModels.Main;

public class TabsViewModel : ViewModelBase
{
    private TabsViewModel() { }

    private static TabsViewModel? _instance;
    public static TabsViewModel GetInstance() => _instance ??= new();

    #region Properties
    
    public ObservableCollection<CollectionTabVM> Tabs { get; } = [];

    public Stack<CodexCollectionVM> ClosedTabs { get; } = [];

    private int _tabIndex = 0;
    public int TabIndex
    {
        get => _tabIndex;
        set
        {
            value = Math.Clamp(value, 0, Tabs.Count - 1);
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
    public RelayCommand CreateTabCommand => _createTabCommand ??= new(CreateTab);
    
    private RelayCommand<CollectionTabVM>? _closeTabCommand;
    public RelayCommand<CollectionTabVM> CloseTabCommand => _closeTabCommand ??= new(CloseTab);
    #endregion

    #region Methods
    
    public void CreateTab()
    {
        var tab = new CollectionTabVM();
        CreateTab(tab);
    }
    
    public void CreateTab(CodexCollectionVM collectionVm)
    {
        var tab = new CollectionTabVM(collectionVm);
        CreateTab(tab);
    }

    private void CreateTab(CollectionTabVM tab)
    {
        TabCreated?.Invoke(this, tab);
        Tabs.Add(tab);
        TabIndex = Tabs.Count - 1;
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
        
        ClosedTabs.Push(tab.CollectionVM);
        
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
        if (!ClosedTabs.Any()) return;
        var collectionVm = ClosedTabs.Pop();
        CreateTab(collectionVm);
    }

    #endregion
}