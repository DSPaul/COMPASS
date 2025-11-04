using Avalonia.Controls;
using COMPASS.Common.ViewModels.Layouts;
using COMPASS.Common.ViewModels.Main;

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
            cb.DataContext is CollectionTabVM vm &&
            e.AddedItems.Count > 0 &&
            e.AddedItems[0] is LayoutViewModel layoutVm)
        {
            vm.ChangeLayoutCommand.Execute(layoutVm.LayoutType);
        }
    }
}