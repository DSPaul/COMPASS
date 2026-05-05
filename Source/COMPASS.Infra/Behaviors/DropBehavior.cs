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

    private static Control? _currentAdorner;
    private static Visual? _currentAdornerOwner;

    private static void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (sender is not Visual dropTarget) return;

        DropManager? manager = FindInheritedValue(dropTarget, DropManagerProperty);
        if (manager is null || !manager.CanHandleDrop(e.DataTransfer))
        {
            e.DragEffects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        var context = new DropContext { DropTarget = dropTarget, DragEventArgs = e };
        UpdateAdorner(dropTarget, manager, e.DataTransfer, context);

        var handler = manager.GetFirstApplicableHandler(e.DataTransfer);
        e.DragEffects = handler?.DropEffects ?? DragDropEffects.None;
        e.Handled = true;
    }

    private static void OnDragOver(object? sender, DragEventArgs e)
    {
        if (sender is not Visual dropTarget) return;

        DropManager? manager = FindInheritedValue(dropTarget, DropManagerProperty);
        var handler = manager?.GetFirstApplicableHandler(e.DataTransfer);

        if (handler is null)
        {
            e.DragEffects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        var context = new DropContext { DropTarget = dropTarget, DragEventArgs = e };
        UpdateAdorner(dropTarget, manager!, e.DataTransfer, context);

        e.DragEffects = handler.DropEffects;
        e.Handled = true;
    }

    private static void OnDragLeave(object? sender, DragEventArgs e)
    {
        ClearAdorner();
        e.Handled = true;
    }

    private static void OnDrop(object? sender, DragEventArgs e)
    {
        ClearAdorner();

        if (sender is not Visual dropTarget) return;

        DropManager? dropManager = FindInheritedValue(dropTarget, DropManagerProperty);
        if (dropManager is null) return;

        var context = new DropContext { DropTarget = dropTarget, DragEventArgs = e };
        dropManager.HandleDrop(e.DataTransfer, context);
        e.Handled = true;
    }

    #endregion

    #region Adorner Management

    private static void UpdateAdorner(Visual dropTarget, DropManager manager, IDataTransfer transfer, DropContext context)
    {
        var adorner = manager.GetAdorner(transfer, context);

        if (adorner is null)
        {
            ClearAdorner();
            return;
        }

        // If the handler returns the same adorner instance, just leave it (it manages its own state)
        if (ReferenceEquals(adorner, _currentAdorner) && ReferenceEquals(dropTarget, _currentAdornerOwner))
            return;

        ClearAdorner();
        _currentAdorner = adorner;
        _currentAdornerOwner = dropTarget;
        AdornerLayer.SetAdorner(dropTarget, adorner);
    }

    private static void ClearAdorner()
    {
        if (_currentAdornerOwner is not null && _currentAdorner is not null)
        {
            AdornerLayer.SetAdorner(_currentAdornerOwner, null);
        }
        _currentAdorner = null;
        _currentAdornerOwner = null;
    }

    #endregion
}

