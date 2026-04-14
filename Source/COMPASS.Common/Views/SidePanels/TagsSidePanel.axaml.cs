using Avalonia.Controls;
using Avalonia.Input;
using COMPASS.Common.Models.DragDrop;
using COMPASS.Common.Models.Hierarchy;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Infra.ExtensionMethods;
using OpenQA.Selenium.DevTools.V143.CSS;

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

    private async void Tag_PointerExited(object? sender, PointerEventArgs e)
    {
        if (e.Properties.IsLeftButtonPressed)
        {
            var dragData = new DataTransfer();

            if (sender is Control control && control.DataContext is TreeNode<TagViewModel> vm)
            {
                dragData.AddTag(vm.Item.GetModel());
            }

            var result = await DragDrop.DoDragDropAsync(e, dragData, DragDropEffects.Move | DragDropEffects.Link);
        }
    }
}