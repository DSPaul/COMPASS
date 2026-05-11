using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using COMPASS.Common.Models.DragDrop;
using COMPASS.Common.Models.Hierarchy;
using COMPASS.Common.Models;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Common.ViewModels.SidePanels;
using COMPASS.Infra.Avalonia.Behaviors;
using COMPASS.Infra.Avalonia.DragDrop;

namespace COMPASS.Common.Views.SidePanels;

public partial class TagsSidePanel : SidePanel
{
    public TagsSidePanel()
    {
        InitializeComponent();

        // Set up the DragManager on the TreeView so drag sources can create reorder payloads
        // and also enrich the transfer with the Tag for cross-component drops.
        var dragManager = new DragManager()
            .AddHandler(new ReorderDragHandler())
            .AddHandler(new DragHandler<Tag>
            {
                DataFormat = DataTransferFormats.TagFormat,
                GetData = visual => (visual?.DataContext as TreeNode<TagViewModel>)?.Item.GetModel(),
                IsDraggable = tag => !tag.IsGroup,
            });

        DragBehavior.SetDragManager(TagTree, dragManager);

        // Set up the DropManager on the TreeView with the reorder handler
        var dropManager = new DropManager()
            .AddHandler(new ReorderDropHandler
            {
                AfterDrop = UpdateTagParent
            });

        DropBehavior.SetDropManager(TagTree, dropManager);
    }

    private static void UpdateTagParent(object draggedItem, object? newParentDataContext, int insertionIndex)
    {
        if (draggedItem is not TreeNode<TagViewModel> draggedNode) return;

        Tag draggedTag = draggedNode.Item.GetModel();
        Tag? oldParentTag = draggedTag.Parent;
        Tag? newParentTag = (newParentDataContext as TreeNode<TagViewModel>)?.Item.GetModel();

        if (ReferenceEquals(oldParentTag, newParentTag)) return;

        var rootTags = TabsViewModel.GetInstance().ActiveTab?.CollectionVM.Collection.RootTags;
        if (rootTags is null) return;

        // Remove from old parent's children or root list
        if (oldParentTag is not null)
            oldParentTag.Children.Remove(draggedTag);
        else
            rootTags.Remove(draggedTag);

        // Insert into new parent's children or root list at the correct position
        if (newParentTag is not null)
        {
            var targetChildren = newParentTag.Children;
            int clampedIndex = Math.Clamp(insertionIndex, 0, targetChildren.Count);
            targetChildren.Insert(clampedIndex, draggedTag);
        }
        else
        {
            int clampedIndex = Math.Clamp(insertionIndex, 0, rootTags.Count);
            rootTags.Insert(clampedIndex, draggedTag);
        }

        // Update the parent reference (triggers tree rebuild via OnTagParentChanged)
        draggedTag.Parent = newParentTag;
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

    private void Tag_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _lastPressedArgs = e;
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
    }
}