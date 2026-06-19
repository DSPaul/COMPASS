using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;
using System.Collections.Specialized;
using static System.Math;

namespace COMPASS.Common.Controls;

/// <summary>
/// Like a WrapPanel but behaves more like a grid. Virtualizes items so that only
/// those within the visible viewport (plus a scroll buffer) are measured and rendered.
/// </summary>
public class WrapGrid : VirtualizingPanel
{
    public static readonly StyledProperty<double> ItemWidthProperty =
        AvaloniaProperty.Register<WrapGrid, double>(nameof(ItemWidth), double.NaN);

    public static readonly StyledProperty<double> ItemHeightProperty =
        AvaloniaProperty.Register<WrapGrid, double>(nameof(ItemHeight), double.NaN);

    public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
        AvaloniaProperty.Register<WrapGrid, HorizontalAlignment>(
            nameof(HorizontalContentAlignment), HorizontalAlignment.Center);

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

    #region Virtualization state

    // Container tracking
    private readonly Dictionary<int, Control> _indexToContainer = new();
    private readonly Dictionary<Control, int> _containerToIndex = new();

    // Recycle pool: recycleKey → stack of hidden containers
    private readonly Dictionary<object, Stack<Control>> _recyclePool = new();
    private const int MaxPoolSize = 20;

    // Per-item layout cache
    private Rect[] _itemBoundsCache = Array.Empty<Rect>();
    private double[] _itemHeightCache = Array.Empty<double>();
    private bool[] _itemHeightKnown = Array.Empty<bool>();
    private int _itemCacheCount;

    // Reusable sets/lists to avoid per-frame allocations
    private readonly HashSet<int> _neededIndices = new();
    private readonly List<int> _toRecycle = new();

    #endregion

    // Layout parameters derived in MeasureOverride, reused in ArrangeOverride
    private int _columnCount = 1;
    private double _cellWidth = 0;
    private double _horizontalOffset = 0;
    private double _lastAvailableWidth = -1;
    private double _estimatedItemHeight = 250; // fallback until first real measure

    // Viewport tracking
    private Rect _viewport;

    static WrapGrid()
    {
        AffectsMeasure<WrapGrid>(ItemWidthProperty, ItemHeightProperty, HorizontalContentAlignmentProperty);
    }

    public WrapGrid()
    {
        EffectiveViewportChanged += OnEffectiveViewportChanged;
    }

    private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
    {
        _viewport = e.EffectiveViewport;
        InvalidateMeasure();
    }


    // Collection change — reset caches when the source list changes
    protected override void OnItemsChanged(IReadOnlyList<object?> items, NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(items, e);

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Reset:
                RecycleAllContainers();
                ClearAllCaches();
                break;

            case NotifyCollectionChangedAction.Remove:
            case NotifyCollectionChangedAction.Replace:
            case NotifyCollectionChangedAction.Move:
                RecycleAllContainers();
                ClearAllCaches();
                break;

            case NotifyCollectionChangedAction.Add:
                // Appending to the end: existing bounds are still valid.
                // Only the new items need entries; we handle that in MeasureOverride.
                if (e.NewStartingIndex < _itemCacheCount)
                {
                    // Inserted in the middle — existing cache is invalid
                    RecycleAllContainers();
                    ClearAllCaches();
                }
                break;
        }

        InvalidateMeasure();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var items = Items;
        int itemCount = items.Count;
        var generator = ItemContainerGenerator;

        if (itemCount == 0 || generator == null)
        {
            RecycleAllContainers();
            return default;
        }

        bool hasFixedWidth = !double.IsNaN(ItemWidth);
        bool hasFixedHeight = !double.IsNaN(ItemHeight);


        // Step 1: Determine cell width
        bool widthChanged = Abs(availableSize.Width - _lastAvailableWidth) > 0.5;

        if (hasFixedWidth)
        {
            _cellWidth = ItemWidth;
        }
        else if (widthChanged || _cellWidth == 0)
        {
            // Materialize a single item to probe its desired width.
            // This is the only measure that happens unconditionally on width change.
            var probe = GetOrCreateContainer(items, 0, generator);
            if (probe != null)
            {
                probe.Measure(new Size(availableSize.Width, double.PositiveInfinity));
                _cellWidth = probe.DesiredSize.Width;
            }
        }

        if (_cellWidth == 0) _cellWidth = availableSize.Width; // fallback

        // Step 2: Derive column count and horizontal offset
        double availableWidth = double.IsInfinity(availableSize.Width)
            ? _cellWidth
            : availableSize.Width;

        _columnCount = Max(1, (int)Floor(availableWidth / _cellWidth));

        _horizontalOffset = HorizontalContentAlignment switch
        {
            HorizontalAlignment.Center => (availableWidth - _cellWidth * _columnCount) / 2,
            HorizontalAlignment.Right => availableWidth - _cellWidth * _columnCount,
            _ => 0
        };
        _horizontalOffset = Max(0, _horizontalOffset);

        _lastAvailableWidth = availableSize.Width;


        // Step 3: Compute bounds for ALL items using cached / estimated heights.
        //         No containers are created here — this is a pure arithmetic pass.
        EnsureCaches(itemCount);

        double y = 0;
        for (int i = 0; i < itemCount; i += _columnCount)
        {
            int rowEnd = Min(i + _columnCount, itemCount);

            // Row height = max of known heights in this row, or estimated
            double rowHeight = hasFixedHeight ? ItemHeight : 0;
            if (!hasFixedHeight)
            {
                for (int j = i; j < rowEnd; j++)
                {
                    double h = _itemHeightKnown[j] ? _itemHeightCache[j] : _estimatedItemHeight;
                    rowHeight = Max(rowHeight, h);
                }
            }

            for (int j = i; j < rowEnd; j++)
            {
                int col = j % _columnCount;
                double x = _horizontalOffset + col * _cellWidth;
                _itemBoundsCache[j] = new Rect(x, y, _cellWidth, rowHeight);
            }

            y += rowHeight;
        }

        double totalHeight = y;

        // Step 4: Determine which items intersect the viewport + buffer
        double vpHeight = _viewport.Height > 0 ? _viewport.Height
                        : (Bounds.Height > 0 ? Bounds.Height : 800);
        double buffer = vpHeight * 1.5;
        double vpTop = Max(0, _viewport.Y - buffer);
        double vpBottom = _viewport.Y + vpHeight + buffer;

        _neededIndices.Clear();
        for (int i = 0; i < itemCount; i++)
        {
            var r = _itemBoundsCache[i];
            if (r.Bottom >= vpTop && r.Y <= vpBottom)
                _neededIndices.Add(i);
        }

        // Step 5: Recycle containers that scrolled out of view
        _toRecycle.Clear();
        foreach (var idx in _indexToContainer.Keys)
            if (!_neededIndices.Contains(idx))
                _toRecycle.Add(idx);
        foreach (var idx in _toRecycle)
            RecycleContainerAt(idx);

        // Step 6: Materialize + measure only visible items.
        //         If a measured height differs from the cached/estimated value,
        //         record it and do a second bounds pass.
        bool needsBoundsRecompute = false;

        foreach (int i in _neededIndices)
        {
            var container = GetOrCreateContainer(items, i, generator);
            if (container == null) continue;

            if (hasFixedHeight)
            {
                container.Measure(new Size(_cellWidth, ItemHeight));
            }
            else
            {
                // Measure with unconstrained height so the item can be its natural size
                container.Measure(new Size(_cellWidth, double.PositiveInfinity));
                double measuredHeight = container.DesiredSize.Height;

                if (!_itemHeightKnown[i] || Abs(_itemHeightCache[i] - measuredHeight) > 1.0)
                {
                    _itemHeightCache[i] = measuredHeight;
                    _itemHeightKnown[i] = true;
                    needsBoundsRecompute = true;
                }
            }
        }

        // _itemCacheCount must be set before UpdateEstimatedHeight so its loop
        // covers all items measured in this pass (not just those from the previous one).
        _itemCacheCount = itemCount;

        // Update the running estimated height from the average of known heights
        // so that items we haven't seen yet get a better estimate.
        UpdateEstimatedHeight();

        // Step 7: If any heights changed, recompute bounds and re-measure
        //         containers that ended up with a different size.
        //         In practice this only fires the first time each item appears.
        if (needsBoundsRecompute && !hasFixedHeight)
        {
            y = 0;
            for (int i = 0; i < itemCount; i += _columnCount)
            {
                int rowEnd = Min(i + _columnCount, itemCount);

                double rowHeight = 0;
                for (int j = i; j < rowEnd; j++)
                {
                    double h = _itemHeightKnown[j] ? _itemHeightCache[j] : _estimatedItemHeight;
                    rowHeight = Max(rowHeight, h);
                }

                for (int j = i; j < rowEnd; j++)
                {
                    int col = j % _columnCount;
                    double x = _horizontalOffset + col * _cellWidth;
                    var newRect = new Rect(x, y, _cellWidth, rowHeight);

                    // Re-measure if the allocated height changed for this item
                    if (_neededIndices.Contains(j) && _itemBoundsCache[j].Height != rowHeight)
                    {
                        if (_indexToContainer.TryGetValue(j, out var c))
                            c.Measure(new Size(_cellWidth, rowHeight));
                    }

                    _itemBoundsCache[j] = newRect;
                }

                y += rowHeight;
            }

            totalHeight = y;
        }

        return new Size(availableWidth, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var kvp in _indexToContainer)
        {
            int idx = kvp.Key;
            var container = kvp.Value;

            if (idx >= 0 && idx < _itemCacheCount)
                container.Arrange(_itemBoundsCache[idx]);
        }

        return finalSize;
    }

    #region Container lifecycle helpers

    private Control? GetOrCreateContainer(IReadOnlyList<object?> items, int index,
                                          ItemContainerGenerator generator)
    {
        if (_indexToContainer.TryGetValue(index, out var existing))
            return existing;

        if (index < 0 || index >= items.Count)
            return null;

        var item = items[index];

        Control container;
        if (generator.NeedsContainer(item, index, out var recycleKey))
        {
            container = GetRecycledContainer(item, index, recycleKey, generator)
                     ?? CreateNewContainer(item, index, recycleKey, generator);
        }
        else
        {
            container = PrepareOwnContainer((Control)item!, item, index, generator);
        }

        _indexToContainer[index] = container;
        _containerToIndex[container] = index;
        container.IsVisible = true;
        return container;
    }

    private Control PrepareOwnContainer(Control control, object? item, int index,
                                        ItemContainerGenerator generator)
    {
        if (!_containerToIndex.ContainsKey(control))
        {
            generator.PrepareItemContainer(control, item, index);
            AddInternalChild(control);
            generator.ItemContainerPrepared(control, item, index);
        }
        return control;
    }

    private Control? GetRecycledContainer(object? item, int index, object? recycleKey,
                                          ItemContainerGenerator generator)
    {
        if (recycleKey is null) return null;
        if (!_recyclePool.TryGetValue(recycleKey, out var pool) || pool.Count == 0) return null;

        var recycled = pool.Pop();
        generator.PrepareItemContainer(recycled, item, index);
        generator.ItemContainerPrepared(recycled, item, index);
        return recycled;
    }

    private Control CreateNewContainer(object? item, int index, object? recycleKey,
                                       ItemContainerGenerator generator)
    {
        var container = generator.CreateContainer(item, index, recycleKey);
        generator.PrepareItemContainer(container, item, index);
        AddInternalChild(container);
        generator.ItemContainerPrepared(container, item, index);
        return container;
    }

    private void RecycleContainerAt(int index)
    {
        if (!_indexToContainer.TryGetValue(index, out var container)) return;

        _indexToContainer.Remove(index);
        _containerToIndex.Remove(container);

        // Ask the generator what recycle key this container has
        if (ItemContainerGenerator is { } generator)
        {
            generator.NeedsContainer(Items.Count > index ? Items[index] : null,
                                     index, out var recycleKey);

            if (recycleKey != null)
            {
                generator.ClearItemContainer(container);
                PushToPool(recycleKey, container);
                container.IsVisible = false;
                return;
            }
        }

        RemoveInternalChild(container);
    }

    private void RecycleAllContainers()
    {
        _toRecycle.Clear();
        _toRecycle.AddRange(_indexToContainer.Keys);
        foreach (var idx in _toRecycle)
            RecycleContainerAt(idx);
    }

    private void PushToPool(object recycleKey, Control container)
    {
        if (!_recyclePool.TryGetValue(recycleKey, out var pool))
        {
            pool = new Stack<Control>();
            _recyclePool[recycleKey] = pool;
        }

        if (pool.Count < MaxPoolSize)
            pool.Push(container);
        else
            RemoveInternalChild(container);
    }
    #endregion

    #region Cache helpers

    private void EnsureCaches(int itemCount)
    {
        if (_itemBoundsCache.Length >= itemCount) return;
        int newSize = Max(itemCount, _itemBoundsCache.Length * 2);
        Array.Resize(ref _itemBoundsCache, newSize);
        Array.Resize(ref _itemHeightCache, newSize);
        Array.Resize(ref _itemHeightKnown, newSize);
    }

    private void ClearAllCaches()
    {
        if (_itemCacheCount > 0)
        {
            Array.Clear(_itemBoundsCache, 0, _itemCacheCount);
            Array.Clear(_itemHeightCache, 0, _itemCacheCount);
            Array.Clear(_itemHeightKnown, 0, _itemCacheCount);
        }
        _itemCacheCount = 0;
        _lastAvailableWidth = -1;
        _estimatedItemHeight = 250;
    }

    private void UpdateEstimatedHeight()
    {
        double sum = 0;
        int count = 0;
        int limit = Min(_itemCacheCount, _itemHeightKnown.Length);
        for (int i = 0; i < limit; i++)
        {
            if (_itemHeightKnown[i])
            {
                sum += _itemHeightCache[i];
                count++;
            }
        }
        if (count > 0)
            _estimatedItemHeight = sum / count;
    }

    #endregion

    #region VirtualizingPanel overrides

    protected override Control? ContainerFromIndex(int index)
    {
        _indexToContainer.TryGetValue(index, out var c);
        return c;
    }

    protected override int IndexFromContainer(Control container) =>
        _containerToIndex.TryGetValue(container, out var idx) ? idx : -1;

    protected override IEnumerable<Control> GetRealizedContainers() =>
        _indexToContainer.Values;

    protected override Control? ScrollIntoView(int index)
    {
        if (index < 0 || index >= Items.Count) return null;

        if (index < _itemCacheCount)
        {
            var container = GetOrCreateContainer(Items, index, ItemContainerGenerator!);
            container?.BringIntoView();
            return container;
        }

        // Bounds not yet computed — estimate the item's position and scroll to it.
        // The viewport change that follows will trigger a proper measure pass.
        int estimatedRow = index / Max(1, _columnCount);
        double estimatedY = estimatedRow * _estimatedItemHeight;
        if (this.FindAncestorOfType<ScrollViewer>() is { } scrollViewer)
            scrollViewer.Offset = new Vector(scrollViewer.Offset.X, estimatedY);
        return null;
    }

    protected override IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
    {
        int count = Items.Count;
        if (count == 0) return null;

        int columns = Max(1, _columnCount);

        if (from is null)
        {
            return direction switch
            {
                NavigationDirection.First or NavigationDirection.Next
                    => ContainerFromIndex(0),
                NavigationDirection.Last or NavigationDirection.Previous
                    => ContainerFromIndex(count - 1),
                _ => ContainerFromIndex(0)
            };
        }

        int currentIndex = IndexFromContainer((Control)from);
        if (currentIndex < 0) return null;

        int row = currentIndex / columns;
        int col = currentIndex % columns;
        int rowCount = (int)Ceiling((double)count / columns);

        int targetIndex = direction switch
        {
            NavigationDirection.Left => col > 0 ? currentIndex - 1 : -1,
            NavigationDirection.Right => col < columns - 1 && currentIndex + 1 < count ? currentIndex + 1 : -1,
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
            if (targetIndex < 0) targetIndex = count - 1;
            else if (targetIndex >= count) targetIndex = 0;
        }

        if (targetIndex >= 0 && targetIndex < count)
            return ContainerFromIndex(targetIndex);

        return null;
    }

    private static int GetUpIndex(int row, int col, int columns, int count, int rowCount, bool wrap)
    {
        if (row > 0) return (row - 1) * columns + col;
        if (!wrap) return -1;

        int lastRowIndex = (rowCount - 1) * columns + col;
        return lastRowIndex < count ? lastRowIndex : (rowCount - 2) * columns + col;
    }

    private static int GetDownIndex(int row, int col, int columns, int count, int rowCount, bool wrap)
    {
        int targetIndex = (row + 1) * columns + col;
        if (targetIndex < count) return targetIndex;
        if (row + 1 < rowCount) return count - 1; // next row exists but this column doesn't

        if (!wrap) return -1;
        return Min(col, count - 1);
    }

    private int GetPageRowCount()
    {
        if (_itemCacheCount == 0) return 1;

        double viewportHeight = Bounds.Height;
        // Use first known height as representative row height
        double cellHeight = _itemHeightKnown.Length > 0 && _itemHeightKnown[0]
            ? _itemHeightCache[0]
            : _estimatedItemHeight;

        return cellHeight > 0 ? Max(1, (int)(viewportHeight / cellHeight)) : 1;
    }
    #endregion
}