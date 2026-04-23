using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using COMPASS.Common.Models;
using COMPASS.Common.Models.DragDrop;
using COMPASS.Common.Models.Filters;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.ModelVMs;

namespace COMPASS.Common.Views.Main;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private CollectionTabVM? ActiveTabVM => DataContext switch
    {
        MainViewModel mainVm => mainVm.TabsVM.ActiveTab,
        TabsViewModel tabsVm => tabsVm.ActiveTab,
        CollectionTabVM tabVm => tabVm,
        _ => null
    };
    private async void UserControl_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.S:
                // Ctrl + S to search
                if (e.KeyModifiers == KeyModifiers.Control)
                {
                    Searchbar.Focus();
                    e.Handled = true;
                }
                break;

            case Key.I:
                // Ctrl + I toggle info
                if (e.KeyModifiers == KeyModifiers.Control && ActiveTabVM != null)
                {
                    ActiveTabVM.CurrentLayout.CodexInfoVM.ShowCodexInfo = !ActiveTabVM.CurrentLayout.CodexInfoVM.ShowCodexInfo;
                    e.Handled = true;
                }
                break;
            case Key.T:
                if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift))
                {
                    TabsViewModel.GetInstance().ReopenTab();
                    e.Handled = true;
                }
                else if (e.KeyModifiers == KeyModifiers.Control)
                {
                    TabsViewModel.GetInstance().CreateTab();
                    e.Handled = true;
                }
                break;

            case Key.F5:
                if (ActiveTabVM != null)
                {
                    await ActiveTabVM.Refresh();
                }
                e.Handled = true;
                break;
        }

    }

    private void LayoutSelection_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb &&
            ActiveTabVM != null &&
            e.AddedItems.Count > 0 &&
            e.AddedItems[0] is Layout layout)
        {
            ActiveTabVM.ChangeLayoutCommand.Execute(layout.LayoutType);
        }
    }

    #region DragDrop

    private PointerPressedEventArgs? _lastPressedArgs;

    private async void Filter_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _lastPressedArgs = e;
        e.Handled = true;
    }

    private async void Filter_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_lastPressedArgs != null && e.Properties.IsLeftButtonPressed)
        {
            var dragData = new DataTransfer();

            if (sender is Control control && control.DataContext is FilterViewModel vm)
            {
                Filter filter = vm.GetModel();
                dragData.AddFilter(filter);
                if(filter is TagFilter tagFilter && tagFilter.FilterValue is TagViewModel tagVm)
                {
                    dragData.AddTag(tagVm.GetModel());
                }
            }

            var result = DragDrop.DoDragDropAsync(_lastPressedArgs, dragData, DragDropEffects.Move | DragDropEffects.Link);
        }
        _lastPressedArgs = null;
        e.Handled = true;
    }

    private void Filter_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Source == _lastPressedArgs?.Source && //Pointer should be on same element as it was pressed on
            sender is Visual visual &&
            visual.DataContext is FilterViewModel filterVm &&
            ActiveTabVM?.FiltersVM is FiltersViewModel filtersVm &&
            filtersVm.RemoveFilterCommand.CanExecute(filterVm))
        {
            filtersVm.RemoveFilterCommand.Execute(filterVm);
        }
        _lastPressedArgs = null;
        e.Handled = true;
    }

    private void IncludedFilters_DragEnter(object? sender, DragEventArgs e) => ActiveTabVM?.FiltersVM.OnDragEnter(sender, e, true);
    private void IncludedFilters_DragOver(object? sender, DragEventArgs e) => ActiveTabVM?.FiltersVM.OnDragOver(e, true);
    private void IncludedFilters_Drop(object? sender, DragEventArgs e) => ActiveTabVM?.FiltersVM.OnDrop(sender, e, true);

    private void ExcludedFilters_DragEnter(object? sender, DragEventArgs e) => ActiveTabVM?.FiltersVM.OnDragEnter(sender, e, false);
    private void ExcludedFilters_DragOver(object? sender, DragEventArgs e) => ActiveTabVM?.FiltersVM.OnDragOver(e, false);
    private void ExcludedFilters_Drop(object? sender, DragEventArgs e) => ActiveTabVM?.FiltersVM.OnDrop(sender, e, false);

    private void Filters_DragLeave(object? sender, DragEventArgs e) => ActiveTabVM?.FiltersVM.OnDragLeave(sender, e);
    #endregion
}