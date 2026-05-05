using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using COMPASS.Infra.Models.DragDrop;
using static COMPASS.Infra.Tools.VisualTreeHelpers;

namespace COMPASS.Infra.Behaviors;

public sealed class DropBehavior : AvaloniaObject
{
    #region Attached Properties

    public static readonly AttachedProperty<bool> IsDropTargetProperty =
        AvaloniaProperty.RegisterAttached<DropBehavior, Control, bool>("IsDropTarget");

    public static bool GetIsDropTarget(Control c) => c.GetValue(IsDropTargetProperty);
    public static void SetIsDropTarget(Control c, bool v) => c.SetValue(IsDropTargetProperty, v);

    public static readonly AttachedProperty<DropManager?> DropManagerProperty =
        AvaloniaProperty.RegisterAttached<DropBehavior, Control, DropManager?>("DropManager");

    public static DropManager? GetDropManager(Control c) => c.GetValue(DropManagerProperty);
    public static void SetDropManager(Control c, DropManager? v) => c.SetValue(DropManagerProperty, v);

    #endregion

    #region Event Wiring

    static DropBehavior()
    {
        IsDropTargetProperty.Changed.AddClassHandler<Control>(OnIsDropTargetChanged);
    }

    private static void OnIsDropTargetChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            DragDrop.SetAllowDrop(control, true);
            control.AddHandler(DragDrop.DragEnterEvent, OnDragEnter, handledEventsToo: false);
            control.AddHandler(DragDrop.DragOverEvent, OnDragOver, handledEventsToo: false);
            control.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave, handledEventsToo: false);
            control.AddHandler(DragDrop.DropEvent, OnDrop, handledEventsToo: false);
        }
        else
        {
            DragDrop.SetAllowDrop(control, false);
            control.RemoveHandler(DragDrop.DragEnterEvent, OnDragEnter);
            control.RemoveHandler(DragDrop.DragOverEvent, OnDragOver);
            control.RemoveHandler(DragDrop.DragLeaveEvent, OnDragLeave);
            control.RemoveHandler(DragDrop.DropEvent, OnDrop);
        }
    }

    #endregion

    #region Drop Handling

    private static void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (sender is not Visual dropTarget) return;

        DropManager? manager = FindInheritedValue(dropTarget, DropManagerProperty);
        var handler = manager?.GetFirstApplicableHandler(e.DataTransfer);
        if (handler is not null)
        {
            var adorner = handler.TryGetAdorner(e.DataTransfer);
            AdornerLayer.SetAdorner(dropTarget, adorner);
        }

        e.Handled = true;
    }

    private static void OnDragOver(object? sender, DragEventArgs e)
    {
        if (sender is not Visual dropTarget) return;

        DropManager? manager = FindInheritedValue(dropTarget, DropManagerProperty);
        var handler = manager?.GetFirstApplicableHandler(e.DataTransfer);

        e.DragEffects = handler?.DropEffects ?? DragDropEffects.None;
        e.Handled = true;
    }

    private static void OnDragLeave(object? sender, DragEventArgs e)
    {
        if (sender is Visual dropTarget)
        {
            AdornerLayer.SetAdorner(dropTarget, null);
        }

        e.Handled = true;
    }

    private static void OnDrop(object? sender, DragEventArgs e)
    {
        if (sender is not Visual dropTarget) return;

        AdornerLayer.SetAdorner(dropTarget, null);

        DropManager? dropManager = FindInheritedValue(dropTarget, DropManagerProperty);
        if (dropManager is null) return;

        dropManager.HandleDrop(e.DataTransfer);
        e.Handled = true;
    }

    #endregion
}
