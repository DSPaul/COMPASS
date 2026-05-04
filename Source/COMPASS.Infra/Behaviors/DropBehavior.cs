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

    public static readonly AttachedProperty<DropHandler?> DropHandlerProperty =
        AvaloniaProperty.RegisterAttached<DropBehavior, Control, DropHandler?>("DropHandler");

    public static DropHandler? GetDropHandler(Control c) => c.GetValue(DropHandlerProperty);
    public static void SetDropHandler(Control c, DropHandler? v) => c.SetValue(DropHandlerProperty, v);

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

        DropHandler? handler = FindInheritedValue(dropTarget, DropHandlerProperty);
        var config = handler?.GetFirstApplicableConfig(e.DataTransfer);
        if (config is not null)
        {
            var adorner = config.TryGetAdorner(e.DataTransfer);
            AdornerLayer.SetAdorner(dropTarget, adorner);
        }

        e.Handled = true;
    }

    private static void OnDragOver(object? sender, DragEventArgs e)
    {
        if (sender is not Visual dropTarget) return;

        DropHandler? handler = FindInheritedValue(dropTarget, DropHandlerProperty);
        var config = handler?.GetFirstApplicableConfig(e.DataTransfer);

        e.DragEffects = config?.DropEffects ?? DragDropEffects.None;
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

        DropHandler? handler = FindInheritedValue(dropTarget, DropHandlerProperty);
        if (handler is null) return;

        handler.HandleDrop(e.DataTransfer);
        e.Handled = true;
    }

    #endregion
}
