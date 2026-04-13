using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.VisualTree;
using COMPASS.Common.ViewModels.Layouts;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.ModelVMs;

namespace COMPASS.Common.Views.Layouts;

public partial class ListLayout : CodexLayoutView
{
    public ListLayout()
    {
        InitializeComponent();
    }

    private void DataGrid_OnKeyDown(object? sender, KeyEventArgs e)
    {
        var operations = TabsViewModel.GetInstance().ActiveTab!.CodexCommands;
        operations.HandleKeyDownOnCodex((sender as DataGrid)?.SelectedItems, e);
    }

    private void DataGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is DataGrid dataGrid && e.AddedItems is { Count: > 0 })
        {
            dataGrid.ScrollIntoView(e.AddedItems[0], null);
        }
    }

    private void DataGrid_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is Control source && source.FindAncestorOfType<DataGridRowsPresenter>() != null &&
           sender is DataGrid dataGrid && dataGrid.SelectedItem is CodexViewModel codexVm && 
           codexVm.OpenCodexCommand.CanExecute(codexVm.GetModel()))
        {
            codexVm.OpenCodexCommand.Execute(codexVm.GetModel());
        }
    }

    private void DataGrid_OnSorting(object? sender, DataGridColumnEventArgs e)
    {
        if (sender is DataGrid dataGrid && 
            dataGrid.DataContext is ListLayoutViewModel vm &&
            e.Column.CanUserSort)
        {
            vm.FiltersVM.UpdateSortProperty(GetSortPropertyName(e.Column));
        }
    }
    
    private string GetSortPropertyName(DataGridColumn column)
    {
        string result = column.SortMemberPath;

        if (string.IsNullOrEmpty(result))
        {
            if (column is DataGridBoundColumn boundColumn)
            {
                if (boundColumn.Binding is Binding binding)
                {
                    result = binding.Path;
                }
                else if (boundColumn.Binding is CompiledBindingExtension compiledBinding)
                {
                    result = compiledBinding.Path.ToString();
                }
            }
        }

        return result;
    }
}