using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;


namespace COMPASS.Infra.Avalonia.VisualTree;

/// <summary>
/// Reusable helpers for walking and querying the Avalonia visual tree,
/// primarily around <see cref="ItemsControl"/> and <see cref="TreeView"/> hierarchies.
/// </summary>
public static class VisualTreeHelpers
{
    extension(ItemsControl itemsControl)
    {
        /// <summary>
        /// Finds the items host <see cref="Panel"/> via the <see cref="ItemsPresenter"/>.
        /// </summary>
        public Panel? FindItemsPanel()
        {
            var presenter = itemsControl.FindDescendantOfType<ItemsPresenter>();
            if (presenter is not null)
            {
                var panel = presenter.FindDescendantOfType<Panel>();
                if (panel is not null) return panel;
            }
            return itemsControl.FindDescendantOfType<Panel>();
        }

        /// <summary>
        /// Forces the UI to rebuild item containers by nulling and reassigning
        /// <see cref="ItemsControl.ItemsSource"/>. Needed because in-place list mutations
        /// (RemoveAt/Insert) don't always trigger a full container rebuild in TreeView.
        /// </summary>
        public void RefreshItemsSource()
        {
            var itemsSource = itemsControl.ItemsSource;
            itemsControl.ItemsSource = null;
            itemsControl.ItemsSource = itemsSource;
        }
    }

    extension(Visual visual)
    {
        /// <summary>
        /// Finds the <see cref="ItemsControl"/> that contains the given visual's item.
        /// For a <see cref="TreeView"/>, skips the node's own <see cref="TreeViewItem"/> visual
        /// (whose ItemsSource is the node's <b>children</b>) and returns its parent.
        /// </summary>
        public ItemsControl? FindContainingItemsControl()
        {
            var directParent = visual.FindAncestorOfType<ItemsControl>(includeSelf: false);
            if (directParent is TreeViewItem)
                return directParent.FindAncestorOfType<ItemsControl>(includeSelf: false);
            return directParent;
        }

        /// <summary>
        /// Walks up from <paramref name="visual"/> to find the nearest value of an attached property.
        /// </summary>
        public T? FindInheritedValue<T>(AttachedProperty<T?> property) where T : class
        {
            Visual? current = visual;
            while (current is not null)
            {
                if (current is Control c)
                {
                    var value = c.GetValue(property);
                    if (value is not null) return value;
                }
                current = current.GetVisualParent();
            }
            return default;
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="visual"/> or any of its visual-tree ancestors
        /// is a <see cref="Control"/> whose <see cref="Control.DataContext"/> is reference-equal
        /// to <paramref name="dataContext"/>.
        /// </summary>
        public bool HasAncestorWithDataContext(object dataContext)
        {
            Visual? current = visual;
            while (current is not null)
            {
                if (current is Control control && ReferenceEquals(control.DataContext, dataContext))
                    return true;
                current = current.GetVisualParent();
            }
            return false;
        }

        /// <summary>
        /// Returns the header height of a visual. For <see cref="TreeViewItem"/>, measures
        /// the <see cref="ContentPresenter"/>; otherwise returns the full bounds height.
        /// </summary>
        public double GetHeaderHeight()
        {
            if (visual is TreeViewItem tvi)
            {
                var header = tvi.FindDescendantOfType<ContentPresenter>();
                if (header is not null)
                    return header.Bounds.Height;
            }
            return visual.Bounds.Height;
        }
    }

    extension(Visual? visual)
    {
        /// <summary>
        /// Returns <c>true</c> if <paramref name="visual"/> is, or is a visual-tree descendant of,
        /// <paramref name="ancestor"/>. Works within a single visual root (does not cross
        /// <see cref="Avalonia.Controls.Primitives.PopupRoot"/> boundaries).
        /// </summary>
        public bool IsDescendantOf(Visual ancestor)
        {
            Visual? current = visual;
            while (current is not null)
            {
                if (ReferenceEquals(current, ancestor)) return true;
                current = current.GetVisualParent();
            }
            return false;
        }
    }

    extension(TreeView treeView)
    {
        /// <summary>
        /// Finds the deepest <see cref="TreeViewItem"/> whose header area contains <paramref name="pointInTreeView"/>.
        /// The point should be in the coordinate space of <paramref name="treeView"/>.
        /// </summary>
        public TreeViewItem? FindTreeViewItemAtPoint(Point pointInTreeView)
            => FindTreeViewItemAtPointRecursive(treeView, pointInTreeView);
    }

    private static TreeViewItem? FindTreeViewItemAtPointRecursive(ItemsControl parent, Point pointInTreeView)
    {
        var panel = parent.FindItemsPanel();
        if (panel is null) return null;

        var translated = parent.TranslatePoint(pointInTreeView, panel);
        if (!translated.HasValue) return null;
        double y = translated.Value.Y;

        for (int i = 0; i < panel.Children.Count; i++)
        {
            if (panel.Children[i] is not TreeViewItem tvi || !tvi.IsVisible)
                continue;

            if (y < tvi.Bounds.Top || y >= tvi.Bounds.Bottom)
                continue;

            double headerH = GetHeaderHeight(tvi);
            if (y < tvi.Bounds.Top + headerH)
                return tvi;

            if (tvi.IsExpanded)
            {
                // Translate point into this child's coordinate space
                var childPoint = panel.TranslatePoint(new Point(0, y), tvi);
                if (childPoint.HasValue)
                {
                    var descendant = FindTreeViewItemAtPointRecursive(tvi, childPoint.Value);
                    if (descendant is not null) return descendant;
                }
            }

            return tvi;
        }

        return null;
    }
}
