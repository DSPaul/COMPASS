using Avalonia.Controls;

namespace COMPASS.Common.Views.SidePanels;

public partial class FiltersSidePanel : SidePanel
{
    public FiltersSidePanel()
    {
        InitializeComponent();
    }

    private void ClearOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb)
        {
            cb.Clear();
        }
    }
}