using Avalonia.Controls;
using COMPASS.Common.Features.Codices;
using System.Collections;

namespace COMPASS.Common.Features.Layouts;

public class CodexLayoutView : UserControl
{
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
}