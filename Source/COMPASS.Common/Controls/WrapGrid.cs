using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;

using static System.Math;

namespace COMPASS.Common.Controls;

/// <summary>
/// Like a wrappanel but behaves more like a grid
/// </summary>
public class WrapGrid : Panel, INavigableContainer
{
    public static readonly StyledProperty<double> ItemWidthProperty =
        AvaloniaProperty.Register<WrapGrid, double>(nameof(ItemWidth), double.NaN);

    public static readonly StyledProperty<double> ItemHeightProperty =
        AvaloniaProperty.Register<WrapGrid, double>(nameof(ItemHeight), double.NaN);

    public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
        AvaloniaProperty.Register<WrapGrid, HorizontalAlignment>(nameof(HorizontalContentAlignment), HorizontalAlignment.Center);

    /// <summary>
    /// Fixed width for each item. If NaN, the width of the widest child is used.
    /// </summary>
    public double ItemWidth
    {
        get => GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    /// <summary>
    /// Fixed height for each item. If NaN, the height of the tallest child in each row is used.
    /// </summary>
    public double ItemHeight
    {
        get => GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    /// <summary>
    /// Horizontal alignment of the grid content within the panel.
    /// </summary>
    public HorizontalAlignment HorizontalContentAlignment
    {
        get => GetValue(HorizontalContentAlignmentProperty);
        set => SetValue(HorizontalContentAlignmentProperty, value);
    }

    private int _columnCount = 1;

    /// <summary>
    /// Gets the current number of columns in the grid.
    /// </summary>
    public int ColumnCount => _columnCount;

    protected override Size MeasureOverride(Size availableSize)
    {
        double itemWidth = ItemWidth;
        double itemHeight = ItemHeight;
        bool hasFixedWidth = !double.IsNaN(itemWidth);
        bool hasFixedHeight = !double.IsNaN(itemHeight);

        var childConstraint = new Size(
            hasFixedWidth ? itemWidth : availableSize.Width,
            hasFixedHeight ? itemHeight : availableSize.Height);

        double maxChildWidth = 0;

        foreach (var child in Children)
        {
            child.Measure(childConstraint);

            double childWidth = hasFixedWidth ? itemWidth : child.DesiredSize.Width;
            maxChildWidth = Max(maxChildWidth, childWidth);
        }

        if (maxChildWidth == 0 || Children.Count == 0)
        {
            _columnCount = 1;
            return default;
        }

        double availableWidth = double.IsInfinity(availableSize.Width) ? maxChildWidth * Children.Count : availableSize.Width;
        _columnCount = Max(1, (int)Floor(availableWidth / maxChildWidth));

        double cellWidth = hasFixedWidth ? itemWidth : maxChildWidth;
        double totalHeight = 0;

        for (int i = 0; i < Children.Count; i += _columnCount)
        {
            double rowHeight = 0;
            int rowEnd = Min(i + _columnCount, Children.Count);
            for (int j = i; j < rowEnd; j++)
            {
                double childHeight = hasFixedHeight ? itemHeight : Children[j].DesiredSize.Height;
                rowHeight = Max(rowHeight, childHeight);
            }
            totalHeight += rowHeight;
        }

        return new Size(
            Min(availableSize.Width, cellWidth * _columnCount),
            totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double itemWidth = ItemWidth;
        double itemHeight = ItemHeight;
        bool hasFixedWidth = !double.IsNaN(itemWidth);
        bool hasFixedHeight = !double.IsNaN(itemHeight);

        double maxChildWidth = 0;

        foreach (var child in Children)
        {
            double childWidth = hasFixedWidth ? itemWidth : child.DesiredSize.Width;
            maxChildWidth = Max(maxChildWidth, childWidth);
        }

        if (maxChildWidth == 0 || Children.Count == 0)
        {
            return finalSize;
        }

        _columnCount = Max(1, (int)Floor(finalSize.Width / maxChildWidth));

        double cellWidth = hasFixedWidth ? itemWidth : maxChildWidth;

        double horizontalOffset = HorizontalContentAlignment switch
        {
            HorizontalAlignment.Center => (finalSize.Width - cellWidth * _columnCount) / 2,
            HorizontalAlignment.Right => finalSize.Width - cellWidth * _columnCount,
            _ => 0
        };
        horizontalOffset = Max(0, horizontalOffset);

        double y = 0;
        for (int i = 0; i < Children.Count; i += _columnCount)
        {
            int rowEnd = Min(i + _columnCount, Children.Count);

            double rowHeight = 0;
            for (int j = i; j < rowEnd; j++)
            {
                double childHeight = hasFixedHeight ? itemHeight : Children[j].DesiredSize.Height;
                rowHeight = Max(rowHeight, childHeight);
            }

            for (int j = i; j < rowEnd; j++)
            {
                int col = j % _columnCount;
                double x = horizontalOffset + col * cellWidth;
                Children[j].Arrange(new Rect(x, y, cellWidth, rowHeight));
            }

            y += rowHeight;
        }

        return finalSize;
    }

    IInputElement? INavigableContainer.GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
    {
        int count = Children.Count;
        if (count == 0)
        {
            return null;
        }

        int columns = Max(1, _columnCount);

        if (from is null)
        {
            return direction switch
            {
                NavigationDirection.First or NavigationDirection.Next => Children[0],
                NavigationDirection.Last or NavigationDirection.Previous => Children[count - 1],
                _ => Children[0]
            };
        }

        int currentIndex = Children.IndexOf((Control)from);
        if (currentIndex < 0)
        {
            return null;
        }

        int row = currentIndex / columns;
        int col = currentIndex % columns;
        int rowCount = (int)Ceiling((double)count / columns);

        int targetIndex = direction switch
        {
            NavigationDirection.Left => currentIndex - 1,
            NavigationDirection.Right => currentIndex + 1,
            NavigationDirection.Up => GetUpIndex(row, col, columns, count, rowCount, wrap),
            NavigationDirection.Down => GetDownIndex(row, col, columns, count, rowCount, wrap),
            NavigationDirection.Previous => currentIndex - 1,
            NavigationDirection.Next => currentIndex + 1,
            NavigationDirection.First => 0,
            NavigationDirection.Last => count - 1,
            NavigationDirection.PageUp => Max(0, currentIndex - columns * GetPageRowCount()),
            NavigationDirection.PageDown => Min(count - 1, currentIndex + columns * GetPageRowCount()),
            _ => -1,
        };

        if (wrap)
        {
            if (targetIndex < 0)
            {
                targetIndex = count - 1;
            }
            else if (targetIndex >= count)
            {
                targetIndex = 0;
            }
        }

        if (targetIndex >= 0 && targetIndex < count)
        {
            return Children[targetIndex];
        }

        return null;
    }

    private static int GetUpIndex(int row, int col, int columns, int count, int rowCount, bool wrap)
    {
        if (row > 0)
        {
            return (row - 1) * columns + col;
        }

        if (!wrap)
        {
            return -1;
        }

        // Wrap to the last row, same column
        int lastRowIndex = (rowCount - 1) * columns + col;
        return lastRowIndex < count ? lastRowIndex : (rowCount - 2) * columns + col;
    }

    private static int GetDownIndex(int row, int col, int columns, int count, int rowCount, bool wrap)
    {
        int targetIndex = (row + 1) * columns + col;

        if (targetIndex < count)
        {
            return targetIndex;
        }

        // Target is beyond the last item
        if (row + 1 < rowCount)
        {
            // There is a next row, but the target column doesn't exist; snap to last item
            return count - 1;
        }

        if (!wrap)
        {
            return -1;
        }

        // Wrap to the first row, same column
        return Min(col, count - 1);
    }

    private int GetPageRowCount()
    {
        if (Children.Count == 0)
        {
            return 1;
        }

        double viewportHeight = Bounds.Height;
        double cellHeight = Children[0].Bounds.Height;

        return cellHeight > 0 ? Max(1, (int)(viewportHeight / cellHeight)) : 1;
    }
}
