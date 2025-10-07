using System;
using Avalonia;
using Avalonia.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace COMPASS.Common.Behaviours
{
    /// <summary>
    /// Attached properties for priority-based column shrinking in Grid
    /// </summary>
    public class ShinkPriority
    {
        /// <summary>
        /// Defines the shrink priority for a column. Lower numbers shrink first.
        /// Columns without this property don't participate in priority shrinking.
        /// </summary>
        public static readonly AttachedProperty<int?> ColumnShrinkPriorityProperty =
            AvaloniaProperty.RegisterAttached<ShinkPriority, ColumnDefinition, int?>(
                "ColumnShrinkPriority");

        public static void SetColumnShrinkPriority(ColumnDefinition element, int? value) => element.SetValue(ColumnShrinkPriorityProperty, value);
        public static int? GetColumnShrinkPriority(ColumnDefinition element) => element.GetValue(ColumnShrinkPriorityProperty);

        /// <summary>
        /// Attach to a Grid to enable priority-based column shrinking
        /// </summary>
        public static readonly AttachedProperty<bool> EnableProperty =
            AvaloniaProperty.RegisterAttached<ShinkPriority, Grid, bool>("Enable");

        public static void SetEnable(Grid element, bool value) => element.SetValue(EnableProperty, value);

        public static bool GetEnable(Grid element) => element.GetValue(EnableProperty);

        /// <summary>
        /// Internal property to store original column definitions
        /// </summary>
        private static readonly AttachedProperty<List<ColumnDefinitionInfo>?> OriginalDefinitionsProperty =
            AvaloniaProperty.RegisterAttached<ShinkPriority, Grid, List<ColumnDefinitionInfo>?>("OriginalDefinitions");

        /// <summary>
        /// Internal property to store active column priority
        /// </summary>
        private static readonly AttachedProperty<int?> ActivePriorityProperty =
            AvaloniaProperty.RegisterAttached<ShinkPriority, Grid, int?>("ActivePriority", 0);

        static ShinkPriority()
        {
            EnableProperty.Changed.AddClassHandler<Grid>(OnEnableChanged);
        }

        private static void OnEnableChanged(Grid grid, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.NewValue is true)
            {
                grid.LayoutUpdated += OnLayoutUpdated;
            }
            else
            {
                grid.LayoutUpdated -= OnLayoutUpdated;
            }
        }

        private static void OnLayoutUpdated(object? sender, EventArgs e)
        {
            if (sender is not Grid grid) return;
            AdjustColumnWidths(grid);
        }

        private static void AdjustColumnWidths(Grid grid)
        {
            // Get original sizes or store on first run
            var columns = grid.GetValue(OriginalDefinitionsProperty);
            if (columns == null)
            {
                columns = grid.ColumnDefinitions.Select(c => new ColumnDefinitionInfo(c)).ToList();
                grid.SetValue(OriginalDefinitionsProperty, columns);
            }

            if (columns.Count == 0) return;

            // Get columns with priorities
            var priorityColumns = columns.Where(c => c.Priority.HasValue).ToList();

            if (priorityColumns.Count == 0)
            {
                // No priority columns, nothing to do
                return;
            }

            // Check if everything fits
            double availableWidth = grid.Bounds.Width + 1; // Add a pixel margin to counter imprecision
            double totalDesiredWidth = columns.Sum(c => c.GetPreferredSize());

            if (totalDesiredWidth < availableWidth) 
            {
                //If room to grow, reset
                if (totalDesiredWidth + 10 < availableWidth && //Only reset if sufficient space too meaningfully grow
                    grid.GetValue(ActivePriorityProperty) > 0) //Only reset if anything has been shrunk
                {
                    foreach (ColumnDefinitionInfo col in columns)
                    {
                        col.RestoreValues();
                    }

                    grid.SetValue(ActivePriorityProperty, 0);
                    grid.InvalidateMeasure();
                }

                return;
            }
            
            // Shrink next priority after current one
            var columnsToShrink = priorityColumns
                .GroupBy(c => c.Priority!.Value)
                .OrderBy(g => g.Key)
                .FirstOrDefault(g => g.Key > grid.GetValue(ActivePriorityProperty));

            if (columnsToShrink == null) return;

            //update current priority
            grid.SetValue(ActivePriorityProperty, columnsToShrink.Key);

            //make the current stars a set width
            //So that they don't grow again to match the shrinking columns
            foreach (ColumnDefinition col in columns.Select(c => c.Definition))
            {
                if (col.Width.IsStar && col.ActualWidth <= col.MinWidth + 1)
                {
                    // Lock to minimum
                    col.Width = new GridLength(col.MinWidth, GridUnitType.Pixel);
                }
            }

            foreach (ColumnDefinitionInfo col in columnsToShrink)
            {
                // Make it star so it can shrink
                col.Definition.Width = new GridLength(1, GridUnitType.Star);
                //give it a max width equal to current with so it can only shrink, not grow
                col.Definition.MaxWidth = col.Definition.ActualWidth;
            }

            grid.InvalidateMeasure();
        }


        private class ColumnDefinitionInfo
        {
            public ColumnDefinitionInfo(ColumnDefinition definition)
            {
                Definition = definition;
                OrigWidth = definition.Width;
                OrigMaxWidth = definition.MaxWidth;
            }

            public ColumnDefinition Definition { get; }

            private GridLength OrigWidth { get; }

            private double OrigMaxWidth { get; }

            public int? Priority => GetColumnShrinkPriority(Definition);

            public double GetPreferredSize()
            {
                if (OrigWidth.IsAuto)
                {
                    //PreferredSize tells us what auto size would be
                    //use hacky way to get it because it is internal
                    var prop = typeof(DefinitionBase).GetProperty("PreferredSize", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (double)prop!.GetValue(Definition)!;
                }

                if (OrigWidth.IsAbsolute)
                {
                    return OrigWidth.Value;
                }

                return Definition.ActualWidth;
            }

            public void RestoreValues()
            {
                Definition.Width = OrigWidth;
                Definition.MaxWidth = OrigMaxWidth;
            }
        }
    }
}