using System.Collections;
using System.Linq;
using Avalonia.Controls;
using COMPASS.Common.Models;
using COMPASS.Common.ViewModels.Layouts;

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
            vm.SelectedCodices = selectedCodices?.Cast<Codex>().ToList() ?? [];
        }
    }
}