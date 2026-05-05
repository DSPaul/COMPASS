using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using static COMPASS.Infra.Tools.VisualTreeHelpers;

namespace COMPASS.Infra.Models.DragDrop;

/// <summary>
/// Drop handler that performs position-dependent reordering within an <see cref="IList"/>-backed
/// <see cref="ItemsControl"/> or <see cref="TreeView"/>.
/// Supports flat lists and tree hierarchies with zone-based insertion (before/inside/after).
/// </summary>
public class ReorderDropHandler : DropHandler<ReorderPayload>
{
    /// <summary>
    /// Optional callback invoked after a successful drop.
    /// Receives (draggedItem, newParentDataContext, insertionIndex).
    /// </summary>
    public Action<object, object?, int>? AfterDrop { get; init; }

    public ReorderDropHandler() : base(ReorderPayload.Format, DragDropEffects.Move)
    {
    }

    public override Control? GetAdorner(IDataTransfer transfer, DropContext context)
    {
        var payload = transfer.TryGetValue(ReorderPayload.Format);
        if (payload is null) return null;

        var dropResult = ResolveDropTarget(context.DropTarget, context.DragEventArgs, payload);
        if (dropResult is null)
        {
            HideDropHint();
            return null;
        }

        CancelPendingDropHintHide();
        ShowDropHint(context.DropTarget, dropResult.Value);

        // We manage our own adorner via the static fields; return it so DropBehavior tracks it
        return _adorner;
    }

    public override bool TryHandleDrop(IDataTransfer transfer, DropContext context)
    {
        CancelPendingDropHintHide();
        HideDropHint();

        var payload = transfer.TryGetValue(ReorderPayload.Format);
        if (payload is null) return false;

        var dropResult = ResolveDropTarget(context.DropTarget, context.DragEventArgs, payload);
        if (dropResult is null) return false;

        var sourceList = payload.SourceList;
        var targetList = dropResult.Value.TargetList;
        int insertionIndex = dropResult.Value.InsertionIndex;

        int oldIndex = sourceList.IndexOf(payload.DraggedItem);
        if (oldIndex < 0) return false;

        if (ReferenceEquals(sourceList, targetList))
        {
            if (oldIndex < insertionIndex) insertionIndex--;
            if (oldIndex == insertionIndex) return false;
            sourceList.RemoveAt(oldIndex);
            sourceList.Insert(insertionIndex, payload.DraggedItem);
        }
        else
        {
            sourceList.RemoveAt(oldIndex);
            targetList.Insert(insertionIndex, payload.DraggedItem);
        }

        //If the item was dropped inside of a tree view item, expand it
        if (dropResult.Value.InsideOf is not null)
            dropResult.Value.InsideOf.IsExpanded = true;

        AfterDrop?.Invoke(payload.DraggedItem, dropResult.Value.NewParentDataContext, insertionIndex);

        // Force the UI to rebuild containers after the in-place list mutation
        RefreshItemsSource(payload.SourceRoot);

        return true;
    }

    #region Drop Target Resolution

    private readonly record struct DropResult(Panel Panel, IList TargetList, int InsertionIndex, TreeViewItem? InsideOf, object? NewParentDataContext);

    private enum DropZone { Before, Inside, After }
    private readonly record struct TreeDropInfo(int InsertionIndex, TreeViewItem? HoveredItem, DropZone Zone);

    private static DropResult? ResolveDropTarget(Visual visual, DragEventArgs e, ReorderPayload payload)
    {
        var treeView = visual.FindAncestorOfType<TreeView>(includeSelf: true);
        if (treeView is not null)
            return ResolveTreeViewDrop(treeView, e, payload);

        return ResolveFlatDrop(visual, e, payload);
    }

    private static DropResult? ResolveTreeViewDrop(TreeView treeView, DragEventArgs e, ReorderPayload payload)
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

    private static DropResult? ResolveFlatDrop(Visual container, DragEventArgs e, ReorderPayload payload)
    {
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
            double headerH = GetHeaderHeight(insideOf);
            double panelY = insideOf.Bounds.Top + headerH;
            var parentPanel = insideOf.GetVisualParent() as Panel;
            double yInContainer = (parentPanel ?? panel).TranslatePoint(new Point(0, panelY), container)?.Y ?? panelY;
            double indentX = GetIndentXForChildOf(insideOf, container);
            SetAdornerLine(container, indentX, yInContainer);
        }
        else
        {
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

    internal static void CancelPendingDropHintHide()
    {
        _hideCts?.Cancel();
        _hideCts?.Dispose();
        _hideCts = null;
    }

    internal static void HideDropHint()
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

    private static double GetIndentXFromPanel(Panel panel, int insertionIndex, Visual container)
    {
        if (panel.Children.Count == 0)
            return 0;

        int neighborIndex = Math.Min(insertionIndex, panel.Children.Count - 1);
        if (panel.Children[neighborIndex] is not TreeViewItem neighborItem)
            return 0;

        return GetContentLeftEdge(neighborItem, container);
    }

    private const double FallbackIndentStep = 24;

    private static double GetIndentXForChildOf(TreeViewItem parent, Visual container)
    {
        var childPanel = FindItemsPanel(parent);
        if (childPanel is { Children.Count: > 0 } && childPanel.Children[0] is TreeViewItem firstChild)
        {
            double x = GetContentLeftEdge(firstChild, container);
            if (x > 0) return x;
        }

        double ownX = GetContentLeftEdge(parent, container);
        var grandparent = parent.FindAncestorOfType<TreeViewItem>(includeSelf: false);
        if (grandparent is not null)
        {
            double parentX = GetContentLeftEdge(grandparent, container);
            return ownX + Math.Max(ownX - parentX, FallbackIndentStep);
        }
        return ownX + FallbackIndentStep;
    }

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

        public double LineX { get; set; }
        public double LineY { get; set; }

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
