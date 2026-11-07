using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using COMPASS.Common.Features.Codices;
using System.Collections;

namespace COMPASS.Common.Features.Layouts;

public class CodexLayoutView : UserControl
{
    private LayoutViewModel? _layoutViewModel;

    public CodexLayoutView()
    {
        DataContextChanged += OnDataContextChanged;
    }

    protected CodexOperations? CodexCommands => (DataContext as LayoutViewModel)?.CodexCommands;

    protected void SelectedCodicesChanged(object? sender, SelectionChangedEventArgs e)
    {
        IList? selectedCodices = sender switch
        {
            ListBox listBox => listBox.SelectedItems,
            DataGrid dataGrid => dataGrid.SelectedItems,
            _ => null
        };
        
        if (sender is Control control && 
            control.DataContext is LayoutViewModel vm)
        {
            vm.SelectedCodices = selectedCodices?.Cast<CodexViewModel>().ToList() ?? [];
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_layoutViewModel is not null)
        {
            _layoutViewModel.SelectionRequested -= OnSelectionRequested;
        }

        _layoutViewModel = DataContext as LayoutViewModel;

        if (_layoutViewModel is not null)
        {
            _layoutViewModel.SelectionRequested += OnSelectionRequested;
        }
    }

    private void OnSelectionRequested(object? sender, CodexSelectionEventArgs eventArgs)
        => SelectCodices(eventArgs.Codices);

    /// <summary>
    /// Replaces the selection of every list and grid in this view with the given codices.
    /// Home shows several lists, so one codex can be selected in more than one of them.
    /// </summary>
    private void SelectCodices(IReadOnlyCollection<CodexViewModel> codicesToSelect)
    {
        HashSet<CodexViewModel> requestedCodices = [.. codicesToSelect];
        List<CodexViewModel> selectedCodices = [];
        Control? focusTarget = null;

        foreach (Visual descendant in this.GetVisualDescendants())
        {
            switch (descendant)
            {
                case ListBox listBox:
                    List<CodexViewModel> selectedInListBox = ApplySelection(listBox.ItemsSource, listBox.SelectedItems, requestedCodices);
                    selectedCodices.AddRange(selectedInListBox);
                    if (selectedInListBox.Count > 0)
                    {
                        listBox.ScrollIntoView(selectedInListBox[0]);
                        focusTarget ??= listBox.ContainerFromItem(selectedInListBox[0]) ?? listBox;
                    }
                    break;

                case DataGrid dataGrid:
                    List<CodexViewModel> selectedInDataGrid = ApplySelection(dataGrid.ItemsSource, dataGrid.SelectedItems, requestedCodices);
                    selectedCodices.AddRange(selectedInDataGrid);
                    if (selectedInDataGrid.Count > 0)
                    {
                        dataGrid.ScrollIntoView(selectedInDataGrid[0], null);
                        focusTarget ??= dataGrid;
                    }
                    break;
            }
        }

        if (_layoutViewModel is not null)
        {
            //Each control reports its own selection, so store what all of them show together
            List<CodexViewModel> combinedSelection = [.. selectedCodices.Distinct()];
            if (combinedSelection.Count > 0)
            {
                _layoutViewModel.SelectedCodices = combinedSelection;
            }
        }

        //Take the keyboard focus, so shortcuts act on the new selection
        focusTarget?.Focus();
    }

    /// <summary>
    /// Selects the codices that the given items actually contain, and clears the rest.
    /// </summary>
    private static List<CodexViewModel> ApplySelection(
        IEnumerable? itemsSource,
        IList? selectedItems,
        IReadOnlySet<CodexViewModel> codicesToSelect)
    {
        if (selectedItems is null) return [];

        List<CodexViewModel> matchingCodices = itemsSource?
            .OfType<CodexViewModel>()
            .Where(codicesToSelect.Contains)
            .ToList() ?? [];

        selectedItems.Clear();

        foreach (CodexViewModel codexVm in matchingCodices)
        {
            selectedItems.Add(codexVm);
        }

        return matchingCodices;
    }
}