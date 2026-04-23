using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using COMPASS.Infra.ExtensionMethods;

namespace COMPASS.Infra.Behaviors;

/// <summary>
/// Reusable drag-and-drop reordering for any <see cref="ItemsControl"/> backed by an <see cref="IList"/>.
/// <para>
/// <b>Usage:</b><br/>
/// 1. On the <see cref="ItemsControl"/> (or its inner items panel wrapper), set
///    <c>behaviors:ReorderDragBehavior.IsDropTarget="True"</c>.<br/>
/// 2. On the root element of each item template, set
///    <c>behaviors:ReorderDragBehavior.IsDragSource="True"</c>.
/// </para>
/// Drop detection works on the container level so gaps between items are valid drop zones.
/// A horizontal indicator line is rendered via the adorner layer to avoid layout shifts.
/// </summary>
public sealed class ReorderDragBehavior : AvaloniaObject
{
    // ── Attached properties ──────────────────────────────────────────

    public static readonly AttachedProperty<bool> IsDragSourceProperty =
        AvaloniaProperty.RegisterAttached<ReorderDragBehavior, Control, bool>("IsDragSource");

    public static bool GetIsDragSource(Control c) => c.GetValue(IsDragSourceProperty);
    public static void SetIsDragSource(Control c, bool v) => c.SetValue(IsDragSourceProperty, v);

    public static readonly AttachedProperty<bool> IsDropTargetProperty =
        AvaloniaProperty.RegisterAttached<ReorderDragBehavior, Control, bool>("IsDropTarget");

    public static bool GetIsDropTarget(Control c) => c.GetValue(IsDropTargetProperty);
    public static void SetIsDropTarget(Control c, bool v) => c.SetValue(IsDropTargetProperty, v);

    // ── Drag-transfer format ─────────────────────────────────────────

    private static readonly DataFormat<DragPayload> DragFormat =
        DataFormat.CreateInProcessFormat<DragPayload>(nameof(ReorderDragBehavior));

    private sealed record DragPayload(IList SourceList, object Item);

    // ── Wiring ───────────────────────────────────────────────────────

    static ReorderDragBehavior()
    {
        IsDragSourceProperty.Changed.AddClassHandler<Control>(OnIsDragSourceChanged);
        IsDropTargetProperty.Changed.AddClassHandler<Control>(OnIsDropTargetChanged);
    }

    private static void OnIsDragSourceChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            control.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, handledEventsToo: false);
            control.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, handledEventsToo: false);
        }
        else
        {
            control.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
            control.RemoveHandler(InputElement.PointerMovedEvent, OnPointerMoved);
        }
    }

    private static void OnIsDropTargetChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            DragDrop.SetAllowDrop(control, true);
            control.AddHandler(DragDrop.DragOverEvent, OnDragOver);
            control.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
            control.AddHandler(DragDrop.DropEvent, OnDrop);
        }
        else
        {
            DragDrop.SetAllowDrop(control, false);
            control.RemoveHandler(DragDrop.DragOverEvent, OnDragOver);
            control.RemoveHandler(DragDrop.DragLeaveEvent, OnDragLeave);
            control.RemoveHandler(DragDrop.DropEvent, OnDrop);
        }
    }

    // ── Drag start (item-level) ──────────────────────────────────────

    private static PointerPressedEventArgs? _lastPressedArgs;
    private static Control? _lastPressedControl;

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        if (!e.GetCurrentPoint(control).Properties.IsLeftButtonPressed) return;

        // Only start a drag if the press did not originate from an interactive
        // child control (e.g. a Button). Walk from the source up to this control
        // and bail out if we encounter a Button along the way.
        for (var v = e.Source as Visual; v is not null && v != control; v = v.GetVisualParent())
        {
            if (v is Button) return;
        }

        _lastPressedArgs = e;
        _lastPressedControl = control;
    }

    private static async void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_lastPressedArgs is null || _lastPressedControl is null) return;
        if (sender is not Control control || control != _lastPressedControl) return;
        if (!e.GetCurrentPoint(control).Properties.IsLeftButtonPressed)
        {
            _lastPressedArgs = null;
            _lastPressedControl = null;
            return;
        }

        var pressedArgs = _lastPressedArgs;
        _lastPressedArgs = null;
        _lastPressedControl = null;

        var itemsControl = FindParentItemsControl(control);
        if (itemsControl?.ItemsSource is not IList list) return;

        var item = control.DataContext;
        if (item is null) return;

        var data = new DataTransfer();
        data.AddData(DragFormat, new DragPayload(list, item));

        await DragDrop.DoDragDropAsync(pressedArgs, data, DragDropEffects.Move);
    }

    // ── Drag over (container-level) ──────────────────────────────────

    private static void OnDragOver(object? sender, DragEventArgs e)
    {
        if (sender is not Control container) return;

        var payload = e.DataTransfer.TryGetValue(DragFormat);
        var itemsControl = FindItemsControl(container);
        if (payload is null || itemsControl?.ItemsSource is not IList list ||
            !ReferenceEquals(list, payload.SourceList))
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        e.DragEffects = DragDropEffects.Move;

        var panel = FindItemsPanel(itemsControl);
        if (panel is null) return;

        int insertionIndex = GetInsertionIndex(panel, e);
        CancelPendingHide();
        ShowIndicator(panel, insertionIndex);
    }

    private static void OnDragLeave(object? sender, DragEventArgs e)
    {
        HideIndicatorDeferred();
    }

    // ── Drop (container-level) ───────────────────────────────────────

    private static void OnDrop(object? sender, DragEventArgs e)
    {
        CancelPendingHide();
        HideIndicator();

        if (sender is not Control container) return;

        var payload = e.DataTransfer.TryGetValue(DragFormat);
        var itemsControl = FindItemsControl(container);
        if (payload is null || itemsControl?.ItemsSource is not IList list ||
            !ReferenceEquals(list, payload.SourceList))
            return;

        var panel = FindItemsPanel(itemsControl);
        if (panel is null) return;

        int insertionIndex = GetInsertionIndex(panel, e);

        int oldIndex = list.IndexOf(payload.Item);
        if (oldIndex < 0) return;

        // Adjust for removal shift
        if (oldIndex < insertionIndex) insertionIndex--;
        if (oldIndex == insertionIndex) return;

        list.RemoveAt(oldIndex);
        list.Insert(insertionIndex, payload.Item);

        // Force refresh on the root ItemsControl that owns this list,
        // so that TemplateBindings to inner ItemsControls are preserved.
        RefreshItemsSource(itemsControl);
    }

    // ── Insertion index calculation ──────────────────────────────────

    private static int GetInsertionIndex(Panel panel, DragEventArgs e)
    {
        var pos = e.GetPosition(panel);
        double y = pos.Y;

        for (int i = 0; i < panel.Children.Count; i++)
        {
            var child = panel.Children[i];
            double childMid = (child.Bounds.Top + child.Bounds.Bottom) / 2;

            if (y < childMid)
                return i;
        }

        return panel.Children.Count; // after the last item
    }

    // ── Adorner-based indicator (no layout impact) ───────────────────

    private static readonly SolidColorBrush IndicatorBrush = new(Color.FromRgb(0x4F, 0xC1, 0xFF));

    private static DropIndicatorAdorner? _adorner;
    private static Panel? _adornerOwner;

    private static void ShowIndicator(Panel panel, int insertionIndex)
    {
        // Compute the Y position in panel coordinates where the line should appear
        double indicatorY;

        if (panel.Children.Count == 0)
        {
            indicatorY = 0;
        }
        else if (insertionIndex >= panel.Children.Count)
        {
            // After the last child
            var last = panel.Children[^1];
            indicatorY = last.Bounds.Bottom;
        }
        else if (insertionIndex == 0)
        {
            // Before the first child
            indicatorY = panel.Children[0].Bounds.Top;
        }
        else
        {
            // Between two children: center the line in the gap
            var above = panel.Children[insertionIndex - 1];
            var below = panel.Children[insertionIndex];
            indicatorY = (above.Bounds.Bottom + below.Bounds.Top) / 2;
        }

        if (_adornerOwner != panel || _adorner is null)
        {
            // Remove old adorner
            if (_adornerOwner is not null && _adorner is not null)
                AdornerLayer.SetAdorner(_adornerOwner, null);

            _adorner = new DropIndicatorAdorner();
            _adornerOwner = panel;
            AdornerLayer.SetAdorner(panel, _adorner);
        }

        _adorner.IndicatorY = indicatorY;
        _adorner.IsVisible = true;
        _adorner.IsHitTestVisible = false;
        _adorner.InvalidateVisual();
    }

    private static CancellationTokenSource? _hideCts;

    private static void CancelPendingHide()
    {
        _hideCts?.Cancel();
        _hideCts?.Dispose();
        _hideCts = null;
    }

    private static async void HideIndicatorDeferred()
    {
        CancelPendingHide();
        _hideCts = new CancellationTokenSource();
        var token = _hideCts.Token;

        try
        {
            // Short delay — if a DragOver arrives in the meantime it will cancel this
            await Task.Delay(50, token);
            if (!token.IsCancellationRequested)
                HideIndicator();
        }
        catch (TaskCanceledException)
        {
            // Expected when CancelPendingHide is called
        }
    }

    private static void HideIndicator()
    {
        CancelPendingHide();

        if (_adorner is not null)
            _adorner.IsVisible = false;

        if (_adornerOwner is not null && _adorner is not null)
        {
            AdornerLayer.SetAdorner(_adornerOwner, null);
            _adorner = null;
            _adornerOwner = null;
        }
    }

    // ── Tree helpers ─────────────────────────────────────────────────

    /// <summary>
    /// Walks up the visual tree from the given <see cref="ItemsControl"/> and finds the
    /// outermost <see cref="ItemsControl"/> that shares the same <see cref="ItemsControl.ItemsSource"/>.
    /// This ensures that refreshing the source doesn't break any TemplateBindings on inner controls.
    /// </summary>
    private static void RefreshItemsSource(ItemsControl itemsControl)
    {
        var root = itemsControl;
        var src = itemsControl.ItemsSource;

        var current = (itemsControl as Visual).GetVisualParent();
        while (current is not null)
        {
            if (current is ItemsControl parent && ReferenceEquals(parent.ItemsSource, src))
                root = parent;
            current = current.GetVisualParent();
        }

        root.ItemsSource = null;
        root.ItemsSource = src;
    }

    private static ItemsControl? FindParentItemsControl(Control control)
    {
        var current = control.GetVisualParent();
        while (current is not null)
        {
            if (current is ItemsControl ic)
                return ic;
            current = current.GetVisualParent();
        }
        return null;
    }

    private static ItemsControl? FindItemsControl(Control container)
    {
        if (container is ItemsControl ic) return ic;
        return container.FindDescendantOfType<ItemsControl>() ?? FindParentItemsControl(container);
    }

    private static Panel? FindItemsPanel(ItemsControl itemsControl)
    {
        return itemsControl.FindDescendantOfType<Panel>();
    }

    // ── Adorner that renders a horizontal line at a given Y ──────────

    private sealed class DropIndicatorAdorner : Control
    {
        public double IndicatorY { get; set; }

        public override void Render(DrawingContext context)
        {
            const double thickness = 2;
            const double arrowSize = 5;
            double width = Bounds.Width;
            double y = IndicatorY;

            var pen = new Pen(IndicatorBrush, thickness);

            // Horizontal line
            context.DrawLine(pen, new Point(0, y), new Point(width, y));

            // Left triangle pointing right ▶
            var leftTriangle = new StreamGeometry();
            using (var ctx = leftTriangle.Open())
            {
                ctx.BeginFigure(new Point(0, y - arrowSize), true);
                ctx.LineTo(new Point(arrowSize, y));
                ctx.LineTo(new Point(0, y + arrowSize));
                ctx.EndFigure(true);
            }
            context.DrawGeometry(IndicatorBrush, null, leftTriangle);

            // Right triangle pointing left ◀
            var rightTriangle = new StreamGeometry();
            using (var ctx = rightTriangle.Open())
            {
                ctx.BeginFigure(new Point(width, y - arrowSize), true);
                ctx.LineTo(new Point(width - arrowSize, y));
                ctx.LineTo(new Point(width, y + arrowSize));
                ctx.EndFigure(true);
            }
            context.DrawGeometry(IndicatorBrush, null, rightTriangle);
        }
    }
}
