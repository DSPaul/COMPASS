using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using COMPASS.Common.ViewModels;
using COMPASS.Common.ViewModels.Layouts;

namespace COMPASS.Common.Views.SidePanels;

public partial class LayoutsSidePanel : SidePanel
{
    public LayoutsSidePanel()
    {
        InitializeComponent();
    }
    
    private void LayoutSelection_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb && 
            cb.DataContext is MainViewModel vm &&
            e.AddedItems.Count > 0 &&
            e.AddedItems[0] is LayoutViewModel layoutVm)
        {
            vm.ChangeLayoutCommand.Execute(layoutVm.LayoutType);
        }
    }
}