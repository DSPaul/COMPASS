using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using COMPASS.Common.Models.DragDrop;
using COMPASS.Common.Models.Hierarchy;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Common.ViewModels.SidePanels;
using COMPASS.Infra.ExtensionMethods;

namespace COMPASS.Common.Views.SidePanels;

public partial class TagsSidePanel : SidePanel
{
    public TagsSidePanel()
    {
        InitializeComponent();
    }

    private PointerPressedEventArgs? _lastPressedArgs;


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

    private async void Tag_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _lastPressedArgs = e;
        e.Handled = true;
    }

    private async void Tag_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_lastPressedArgs != null && e.Properties.IsLeftButtonPressed)
        {
            var dragData = new DataTransfer();

            if (sender is Control control && control.DataContext is TreeNode<TagViewModel> vm)
            {
                dragData.AddTag(vm.Item.GetModel());
            }

            var result = DragDrop.DoDragDropAsync(_lastPressedArgs, dragData, DragDropEffects.Move | DragDropEffects.Link);
        }
        _lastPressedArgs = null;
        e.Handled = true;
    }

    private void Tag_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Source == _lastPressedArgs?.Source && //Pointer should be on same element as it was pressed on
            sender is Control control && 
            control.DataContext is TreeNode<TagViewModel> nodeVm &&
            control.FindAncestorOfType<TreeView>() is TreeView tv &&
            tv.DataContext is TagsPanelVM panelVm &&
            panelVm.AddTagFilterCommand.CanExecute(nodeVm.Item))
        {
            panelVm.AddTagFilterCommand.Execute(nodeVm.Item);
        }
        _lastPressedArgs = null;
        e.Handled = true;
    }
}