using Avalonia.Controls;
using COMPASS.Common.ViewModels.Layouts;
using COMPASS.Common.ViewModels.ModelVMs;
using System.Collections;

namespace COMPASS.Common.Views.Layouts;

public class CodexLayoutView : UserControl
{
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