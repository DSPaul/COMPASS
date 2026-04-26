using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using COMPASS.Infra.ExtensionMethods;
using static COMPASS.Infra.Tools.VisualTreeHelpers;

namespace COMPASS.Infra.Behaviors;


/// <summary>
/// Reusable drag-and-drop reordering for any <see cref="ItemsControl"/> (including <see cref="TreeView"/>) backed by an <see cref="IList"/>.
/// <para>
/// <b>Usage:</b><br/>
/// 1. On the <see cref="ItemsControl"/> or <see cref="TreeView"/>, set
///    <c>behaviors:ReorderDragBehavior.IsDropTarget="True"</c>.<br/>
/// 2. On the root element of each item template, set
///    <c>behaviors:ReorderDragBehavior.IsDragSource="True"</c>.
/// </para>
/// <para>
/// For <see cref="TreeView"/>, items can be reordered across nesting levels. Hovering over the
/// top/bottom 25% of a header inserts as a sibling; hovering over the middle 50% inserts as the
/// first child. A drop indicator line (with indentation) or a highlight rectangle (for
/// collapsed / childless nodes) is rendered via the adorner layer.
/// </para>
/// </summary>
public sealed class ReorderDragBehavior : AvaloniaObject
{
    #region Attached Properties

    public static readonly AttachedProperty<bool> IsDragSourceProperty =
        AvaloniaProperty.RegisterAttached<ReorderDragBehavior, Control, bool>("IsDragSource");

    public static bool GetIsDragSource(Control c) => c.GetValue(IsDragSourceProperty);
    public static void SetIsDragSource(Control c, bool v) => c.SetValue(IsDragSourceProperty, v);

    public static readonly AttachedProperty<bool> IsDropTargetProperty =
        AvaloniaProperty.RegisterAttached<ReorderDragBehavior, Control, bool>("IsDropTarget");

    public static bool GetIsDropTarget(Control c) => c.GetValue(IsDropTargetProperty);
    public static void SetIsDropTarget(Control c, bool v) => c.SetValue(IsDropTargetProperty, v);

    /// <summary>
    /// Optional callback that enriches the <see cref="DataTransfer"/> with additional data.
    /// The callback receives the <see cref="DataTransfer"/> and the source draggedVisual's <see cref="Control.DataContext"/>.
    /// Set on an ancestor (e.g. the <see cref="TreeView"/>) — it is inherited by all drag sources.
    /// </summary>
    public static readonly AttachedProperty<Action<DataTransfer, object?>?> DragDataProviderProperty =
        AvaloniaProperty.RegisterAttached<ReorderDragBehavior, Control, Action<DataTransfer, object?>?>("DragDataProvider");

    public static Action<DataTransfer, object?>? GetDragDataProvider(Control c) => c.GetValue(DragDataProviderProperty);
    public static void SetDragDataProvider(Control c, Action<DataTransfer, object?>? v) => c.SetValue(DragDataProviderProperty, v);

    /// <summary>
    /// Optional callback invoked after a successful drop. Receives the dragged item, the
    /// new parent's <see cref="Control.DataContext"/> (null when dropped at root level),
    /// and the insertion index within the new parent's children.
    /// Use this to update parent–child relationships in the view-model layer.
    /// Set on the <see cref="TreeView"/> or <see cref="ItemsControl"/>.
    /// </summary>
    public static readonly AttachedProperty<Action<object, object?, int>?> AfterDropProperty =
        AvaloniaProperty.RegisterAttached<ReorderDragBehavior, Control, Action<object, object?, int>?>("AfterDrop");

    public static Action<object, object?, int>? GetAfterDrop(Control c) => c.GetValue(AfterDropProperty);
    public static void SetAfterDrop(Control c, Action<object, object?, int>? v) => c.SetValue(AfterDropProperty, v);

    #endregion

    #region Drag Payload

    private static readonly DataFormat<DragPayload> DragFormat =
        DataFormat.CreateInProcessFormat<DragPayload>(nameof(ReorderDragBehavior));

    private sealed record DragPayload(IList SourceList, object DraggedItem, ItemsControl SourceRoot);

    #endregion

    #region Event Wiring

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

    #endregion

    #region Drag Start

    private static PointerPressedEventArgs? _lastPressedArgs;
    private static Control? _lastPressedControl;

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        //Middle and left mouse click and such also trigger pointer pressed
        if (!e.Properties.IsLeftButtonPressed) return;

        // Skip if the press originated from an interactive child (e.g. a Button).
        var ancestor = e.Source as Visual;
        while (ancestor is not null && ancestor != control)
        {
            if (ancestor is Button) return;
            ancestor = ancestor.GetVisualParent();
        }

        _lastPressedArgs = e;
        _lastPressedControl = control;
    }

    private static async void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_lastPressedArgs is null || _lastPressedControl is null) return;
        if (sender is not Visual draggedVisual || draggedVisual != _lastPressedControl) return;
            
        var pressedArgs = _lastPressedArgs;
        _lastPressedArgs = null;
        _lastPressedControl = null;
        
        if (!e.Properties.IsLeftButtonPressed) return;

        var containingItemsControl = FindContainingItemsControl(draggedVisual);
        if (containingItemsControl?.ItemsSource is not IList itemsList) return;

        var item = draggedVisual.DataContext;
        if (item is null) return;

        ItemsControl sourceRoot = draggedVisual.FindAncestorOfType<TreeView>(includeSelf: true) ?? containingItemsControl;

        var dragData = new DataTransfer();
        dragData.AddData(DragFormat, new DragPayload(itemsList, item, sourceRoot));

        // Allow consumers to add additional drag data (e.g. a Tag payload for cross-component drops)
        var dataProvider = FindInheritedValue(draggedVisual, DragDataProviderProperty);
        dataProvider?.Invoke(dragData, draggedVisual.DataContext);

        await DragDrop.DoDragDropAsync(pressedArgs, dragData, DragDropEffects.Move | DragDropEffects.Link);
    }

    #endregion

    #region Drag Over / Drop

    private static void OnDragOver(object? sender, DragEventArgs e)
    {
        if (sender is not Visual container) return;

        var dragPayload = e.DataTransfer.TryGetValue(DragFormat);
        if (dragPayload is null)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        var dropTarget = ResolveDropTarget(container, e, dragPayload);
        if (dropTarget is null)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        e.DragEffects = DragDropEffects.Move;

        CancelPendingDropHintHide();
        ShowDropHint(container, dropTarget.Value);
    }

    private static void OnDragLeave(object? sender, DragEventArgs e) => HideIndicatorDeferred();

    private static void OnDrop(object? sender, DragEventArgs e)
    {
        CancelPendingDropHintHide();
        HideDropHint();

        if (sender is not Visual visual) return;

        var dragPayload = e.DataTransfer.TryGetValue(DragFormat);
        if (dragPayload is null) return;

        var dropTarget = ResolveDropTarget(visual, e, dragPayload);
        if (dropTarget is null) return;

        var sourceList = dragPayload.SourceList;
        var targetList = dropTarget.Value.TargetList;
        int insertionIndex = dropTarget.Value.InsertionIndex;

        int oldIndex = sourceList.IndexOf(dragPayload.DraggedItem);
        if (oldIndex < 0) return;

        if (ReferenceEquals(sourceList, targetList))
        {
            if (oldIndex < insertionIndex) insertionIndex--;
            if (oldIndex == insertionIndex) return;
            sourceList.RemoveAt(oldIndex);
            sourceList.Insert(insertionIndex, dragPayload.DraggedItem);
        }
        else
        {
            sourceList.RemoveAt(oldIndex);
            targetList.Insert(insertionIndex, dragPayload.DraggedItem);
        }

        //If the item was dropped inside of a tree view item, expand it
        if (dropTarget.Value.InsideOf is not null)
            dropTarget.Value.InsideOf.IsExpanded = true;

        // Notify consumers so they can update parent–child relationships in the view-model layer
        var afterDrop = FindInheritedValue(visual, AfterDropProperty);
        afterDrop?.Invoke(dragPayload.DraggedItem, dropTarget.Value.NewParentDataContext, insertionIndex);

        // Force the UI to rebuild containers after the in-place list mutation
        RefreshItemsSource(dragPayload.SourceRoot);
    }

    #endregion

    #region Drop Target Resolution

    private readonly record struct DropResult(Panel Panel, IList TargetList, int InsertionIndex, TreeViewItem? InsideOf, object? NewParentDataContext);

    private enum DropZone { Before, Inside, After }
    private readonly record struct TreeDropInfo(int InsertionIndex, TreeViewItem? HoveredItem, DropZone Zone);

    private static DropResult? ResolveDropTarget(Visual visual, DragEventArgs e, DragPayload payload)
    {
        var treeView = visual.FindAncestorOfType<TreeView>(includeSelf: true);
        if (treeView is not null)
            return ResolveTreeViewDrop(treeView, e, payload);

        return ResolveFlatDrop(visual, e, payload);
    }

    private static DropResult? ResolveTreeViewDrop(TreeView treeView, DragEventArgs e, DragPayload payload)
    {
        if (!ReferenceEquals(treeView, payload.SourceRoot))
            return null;

        var hoveredItem = FindTreeViewItemAtPoint(treeView, e.GetPosition(treeView));

        // Prevent dropping onto self or into own descendants (would create a cycle)
        if (hoveredItem is not null && HasAncestorWithDataContext(hoveredItem, payload.DraggedItem))
            return null;

        var parent = hoveredItem?.FindAncestorOfType<ItemsControl>(includeSelf: false) ?? treeView;

        if (parent.ItemsSource is not IList siblings)
            return null;

        var siblingPanel = FindItemsPanel(parent);
        if (siblingPanel is null) return null;

        var dropZoneInfo = GetTreeDropInfo(siblingPanel, e);

        // Middle zone → insert as first child of the hovered node
        if (dropZoneInfo.Zone == DropZone.Inside && dropZoneInfo.HoveredItem is TreeViewItem insideTvi
            && insideTvi.ItemsSource is IList children)
        {
            var childPanel = FindItemsPanel(insideTvi);
            return new DropResult(childPanel ?? siblingPanel, children, 0, insideTvi, insideTvi.DataContext);
        }

        // Bottom zone on an expanded node with children → insert as first child
        // (avoids visually skipping all descendants)
        if (dropZoneInfo.Zone == DropZone.After
            && dropZoneInfo.InsertionIndex > 0 && dropZoneInfo.InsertionIndex <= siblingPanel.Children.Count
            && siblingPanel.Children[dropZoneInfo.InsertionIndex - 1] is TreeViewItem { IsExpanded: true } expandedTvi
            && expandedTvi.ItemsSource is IList expandedChildren && expandedChildren.Count > 0)
        {
            var childPanel = FindItemsPanel(expandedTvi);
            if (childPanel is not null)
                return new DropResult(childPanel, expandedChildren, 0, null, expandedTvi.DataContext);
        }

        object? newParentDataContext = parent is TreeViewItem tvi ? tvi.DataContext : null;
        return new DropResult(siblingPanel, siblings, dropZoneInfo.InsertionIndex, null, newParentDataContext);
    }

    private static DropResult? ResolveFlatDrop(Visual container, DragEventArgs e, DragPayload payload)
    {
        // The IsDropTarget control should be the ItemsControl itself for flat lists
        if (container is not ItemsControl itemsControl) return null;
        if (itemsControl.ItemsSource is not IList items || !ReferenceEquals(items, payload.SourceList))
            return null;

        var panel = FindItemsPanel(itemsControl);
        if (panel is null) return null;

        return new DropResult(panel, items, GetInsertionIndex(panel, e), null, null);
    }

    #endregion

    #region Insertion Index Calculation

    private static int GetInsertionIndex(Panel panel, DragEventArgs e)
    {
        var pos = e.GetPosition(panel);
        for (int i = 0; i < panel.Children.Count; i++)
        {
            double childMid = (panel.Children[i].Bounds.Top + panel.Children[i].Bounds.Bottom) / 2;
            if (pos.Y < childMid)
                return i;
        }
        return panel.Children.Count;
    }

    /// <summary>
    /// Determines the insertion index and drop zone for a TreeView items panel.
    /// The header is divided into three zones: top 25% (before), middle 50% (inside), bottom 25% (after).
    /// </summary>
    private static TreeDropInfo GetTreeDropInfo(Panel panel, DragEventArgs e)
    {
        var pos = e.GetPosition(panel);
        double y = pos.Y;

        for (int i = 0; i < panel.Children.Count; i++)
        {
            var child = panel.Children[i];
            double headerH = GetHeaderHeight(child);
            double headerTop = child.Bounds.Top;
            double headerBottom = headerTop + headerH;

            if (y < headerTop || y >= child.Bounds.Bottom)
                continue;

            // Past the header → expanded children area (handled by recursion elsewhere)
            if (y >= headerBottom)
                return new TreeDropInfo(i + 1, child as TreeViewItem, DropZone.After);

            double quarter = headerH * 0.25;

            if (y < headerTop + quarter)
                return new TreeDropInfo(i, child as TreeViewItem, DropZone.Before);

            if (y > headerBottom - quarter)
                return new TreeDropInfo(i + 1, child as TreeViewItem, DropZone.After);

            return new TreeDropInfo(i, child as TreeViewItem, DropZone.Inside);
        }

        return new TreeDropInfo(panel.Children.Count, null, DropZone.After);
    }

    #endregion

    #region Drop Indicator

    private static readonly SolidColorBrush IndicatorBrush = new(Color.FromRgb(0x4F, 0xC1, 0xFF));

    private static DropIndicatorAdorner? _adorner;
    private static Visual? _adornerOwner;
    private static CancellationTokenSource? _hideCts;

    private static void ShowDropHint(Visual container, DropResult result)
    {
        if (result.InsideOf is not null)
        {
            ShowInsideIndicator(container, result.Panel, result.InsideOf);
            return;
        }

        ShowLineIndicator(container, result.Panel, result.InsertionIndex);
    }

    private static void ShowInsideIndicator(Visual container, Panel panel, TreeViewItem insideOf)
    {
        bool hasVisibleChildren = insideOf is { IsExpanded: true }
                                  && insideOf.ItemsSource is IList { Count: > 0 };

        if (hasVisibleChildren)
        {
            // Show an indented line below the header
            double headerH = GetHeaderHeight(insideOf);
            double panelY = insideOf.Bounds.Top + headerH;
            var parentPanel = insideOf.GetVisualParent() as Panel;
            double yInContainer = (parentPanel ?? panel).TranslatePoint(new Point(0, panelY), container)?.Y ?? panelY;
            double indentX = GetIndentXForChildOf(insideOf, container);
            SetAdornerLine(container, indentX, yInContainer);
        }
        else
        {
            // Show a highlight rectangle around the header
            var headerPresenter = insideOf.FindDescendantOfType<ContentPresenter>();
            if (headerPresenter is not null)
            {
                var topLeft = headerPresenter.TranslatePoint(new Point(0, 0), container);
                if (topLeft.HasValue)
                {
                    SetAdornerRect(container, topLeft.Value.X, topLeft.Value.Y,
                        headerPresenter.Bounds.Width, headerPresenter.Bounds.Height);
                    return;
                }
            }

            // Fallback: indented line below the header
            double headerH = GetHeaderHeight(insideOf);
            double panelY = insideOf.Bounds.Top + headerH;
            var parentPanel = insideOf.GetVisualParent() as Panel;
            double yInContainer = (parentPanel ?? panel).TranslatePoint(new Point(0, panelY), container)?.Y ?? panelY;
            double indentX = GetIndentXForChildOf(insideOf, container);
            SetAdornerLine(container, indentX, yInContainer);
        }
    }

    private static void ShowLineIndicator(Visual container, Panel panel, int insertionIndex)
    {
        double panelY;
        if (panel.Children.Count == 0)
        {
            panelY = 0;
        }
        else if (insertionIndex >= panel.Children.Count)
        {
            panelY = panel.Children[^1].Bounds.Bottom;
        }
        else if (insertionIndex == 0)
        {
            panelY = panel.Children[0].Bounds.Top;
        }
        else
        {
            var above = panel.Children[insertionIndex - 1];
            var below = panel.Children[insertionIndex];
            panelY = (above.Bounds.Bottom + below.Bounds.Top) / 2;
        }

        double yInContainer = panel.TranslatePoint(new Point(0, panelY), container)?.Y ?? panelY;
        double indentX = GetIndentXFromPanel(panel, insertionIndex, container);

        SetAdornerLine(container, indentX, yInContainer);
    }

    private static void EnsureAdorner(Visual container)
    {
        if (_adornerOwner != container || _adorner is null)
        {
            if (_adornerOwner is not null && _adorner is not null)
                AdornerLayer.SetAdorner(_adornerOwner, null);

            _adorner = new DropIndicatorAdorner();
            _adornerOwner = container;
            AdornerLayer.SetAdorner(_adornerOwner, _adorner);
        }
    }

    private static void SetAdornerLine(Visual container, double x, double y)
    {
        EnsureAdorner(container);
        _adorner!.Mode = DropIndicatorAdorner.DisplayMode.Line;
        _adorner.LineX = x;
        _adorner.LineY = y;
        _adorner.IsVisible = true;
        _adorner.IsHitTestVisible = false;
        _adorner.InvalidateVisual();
    }

    private static void SetAdornerRect(Visual container, double x, double y, double width, double height)
    {
        EnsureAdorner(container);
        _adorner!.Mode = DropIndicatorAdorner.DisplayMode.Rect;
        _adorner.HighlightRect = new Rect(x, y, width, height);
        _adorner.IsVisible = true;
        _adorner.IsHitTestVisible = false;
        _adorner.InvalidateVisual();
    }

    private static void CancelPendingDropHintHide()
    {
        _hideCts?.Cancel();
        _hideCts?.Dispose();
        _hideCts = null;
    }

    private static async void HideIndicatorDeferred()
    {
        CancelPendingDropHintHide();
        _hideCts = new CancellationTokenSource();
        var token = _hideCts.Token;

        try
        {
            await Task.Delay(50, token);
            if (!token.IsCancellationRequested)
                HideDropHint();
        }
        catch (TaskCanceledException) { }
    }

    private static void HideDropHint()
    {
        CancelPendingDropHintHide();

        if (_adorner is not null)
            _adorner.IsVisible = false;

        if (_adornerOwner is not null && _adorner is not null)
        {
            AdornerLayer.SetAdorner(_adornerOwner, null);
            _adorner = null;
            _adornerOwner = null;
        }
    }

    // —— Indent calculation ——

    /// <summary>
    /// Returns the X offset matching the content indent level of existing items in <paramref name="panel"/>.
    /// </summary>
    private static double GetIndentXFromPanel(Panel panel, int insertionIndex, Visual container)
    {
        if (panel.Children.Count == 0)
            return 0;

        int neighborIndex = Math.Min(insertionIndex, panel.Children.Count - 1);
        if (panel.Children[neighborIndex] is not TreeViewItem neighborItem)
            return 0;

        return GetContentLeftEdge(neighborItem, container);
    }

    /// <summary>
    /// Returns the X offset one indent level deeper than <paramref name="parent"/>'s content.
    /// Measures from the first visible child if available, otherwise estimates from the indent step.
    /// </summary>
    private const double FallbackIndentStep = 24;

    private static double GetIndentXForChildOf(TreeViewItem parent, Visual container)
    {
        // Try measuring from an existing child
        var childPanel = FindItemsPanel(parent);
        if (childPanel is { Children.Count: > 0 } && childPanel.Children[0] is TreeViewItem firstChild)
        {
            double x = GetContentLeftEdge(firstChild, container);
            if (x > 0) return x;
        }

        // Estimate: own X + indent step (measured from own itemsParent)
        double ownX = GetContentLeftEdge(parent, container);
        var grandparent = parent.FindAncestorOfType<TreeViewItem>(includeSelf: false);
        if (grandparent is not null)
        {
            double parentX = GetContentLeftEdge(grandparent, container);
            return ownX + Math.Max(ownX - parentX, FallbackIndentStep);
        }
        return ownX + FallbackIndentStep;
    }

    /// <summary>
    /// Returns the left edge X of a <see cref="TreeViewItem"/>'s header content in visual coordinates.
    /// </summary>
    private static double GetContentLeftEdge(TreeViewItem tvi, Visual container)
    {
        var presenter = tvi.FindDescendantOfType<ContentPresenter>();
        if (presenter is null) return 0;
        return presenter.TranslatePoint(new Point(0, 0), container)?.X ?? 0;
    }

    #endregion

    #region DropIndicatorAdorner

    private sealed class DropIndicatorAdorner : Control
    {
        public enum DisplayMode { Line, Rect }

        public DisplayMode Mode { get; set; } = DisplayMode.Line;

        // Line mode
        public double LineX { get; set; }
        public double LineY { get; set; }

        // Rect mode
        public Rect HighlightRect { get; set; }

        public override void Render(DrawingContext context)
        {
            if (Mode == DisplayMode.Rect)
                RenderRect(context);
            else
                RenderLine(context);
        }

        private void RenderLine(DrawingContext context)
        {
            const double thickness = 2;
            const double arrowSize = 5;
            double width = Bounds.Width;
            double y = LineY;
            double left = LineX;

            var pen = new Pen(IndicatorBrush, thickness);

            context.DrawLine(pen, new Point(left, y), new Point(width, y));

            // Left arrow ▶
            var leftArrow = new StreamGeometry();
            using (var ctx = leftArrow.Open())
            {
                ctx.BeginFigure(new Point(left, y - arrowSize), true);
                ctx.LineTo(new Point(left + arrowSize, y));
                ctx.LineTo(new Point(left, y + arrowSize));
                ctx.EndFigure(true);
            }
            context.DrawGeometry(IndicatorBrush, null, leftArrow);

            // Right arrow ◀
            var rightArrow = new StreamGeometry();
            using (var ctx = rightArrow.Open())
            {
                ctx.BeginFigure(new Point(width, y - arrowSize), true);
                ctx.LineTo(new Point(width - arrowSize, y));
                ctx.LineTo(new Point(width, y + arrowSize));
                ctx.EndFigure(true);
            }
            context.DrawGeometry(IndicatorBrush, null, rightArrow);
        }

        private void RenderRect(DrawingContext context)
        {
            const double thickness = 2;
            const double cornerRadius = 4;
            var pen = new Pen(IndicatorBrush, thickness);
            context.DrawRectangle(null, pen, HighlightRect, cornerRadius, cornerRadius);
        }
    }

    #endregion
}
