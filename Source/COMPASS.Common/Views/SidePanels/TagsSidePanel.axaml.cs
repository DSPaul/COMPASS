using Avalonia.Controls;
using Avalonia.Input;

namespace COMPASS.Common.Views.SidePanels;

public partial class TagsSidePanel : SidePanel
{
    public TagsSidePanel()
    {
        InitializeComponent();
    }

    private void Toggle_ContextMenu(object sender, TappedEventArgs e)
    {
        var ctxMenu = ((Button)sender).ContextMenu;

        if (ctxMenu == null) return;

        ctxMenu.PlacementTarget = (Button)sender;

        if (ctxMenu.IsOpen)
        {
            ctxMenu.Close();
        }
        else
        {
            ctxMenu.Open();
        }
    }
}