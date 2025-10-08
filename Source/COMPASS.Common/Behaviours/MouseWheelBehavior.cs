using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System;
using Avalonia.Controls.Primitives;

namespace COMPASS.Common.Behaviours
{
    /// <summary>
    /// Defines the scroll direction for mouse wheel behavior.
    /// </summary>
    public enum MouseWheelScrollDirection
    {
        /// <summary>
        /// Default vertical scrolling behavior.
        /// </summary>
        Vertical,
        
        /// <summary>
        /// Redirects vertical mouse wheel to horizontal scrolling.
        /// </summary>
        Horizontal
    }

    /// <summary>
    /// Attached property that controls mouse wheel scrolling direction.
    /// </summary>
    public static class MouseWheelBehavior
    {
        public static readonly AttachedProperty<MouseWheelScrollDirection> ScrollDirectionProperty =
            AvaloniaProperty.RegisterAttached<Control, MouseWheelScrollDirection>(
                "ScrollDirection",
                typeof(MouseWheelBehavior),
                defaultValue: MouseWheelScrollDirection.Vertical);

        public static MouseWheelScrollDirection GetScrollDirection(Control control)
        {
            return control.GetValue(ScrollDirectionProperty);
        }

        public static void SetScrollDirection(Control control, MouseWheelScrollDirection value)
        {
            control.SetValue(ScrollDirectionProperty, value);
        }

        static MouseWheelBehavior()
        {
            ScrollDirectionProperty.Changed.AddClassHandler<Control>(OnScrollDirectionChanged);
        }

        private static void OnScrollDirectionChanged(Control control, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.OldValue is MouseWheelScrollDirection oldDirection && oldDirection == MouseWheelScrollDirection.Horizontal)
            {
                control.PointerWheelChanged -= OnPointerWheelChanged;
            }

            if (e.NewValue is MouseWheelScrollDirection newDirection && newDirection == MouseWheelScrollDirection.Horizontal)
            {
                control.PointerWheelChanged += OnPointerWheelChanged;
            }
        }

        private static void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            //Check if vertical scroll should be converted to horizontal
            if (sender is not Control control || 
                control.GetValue(ScrollDirectionProperty) == MouseWheelScrollDirection.Vertical ||
                e.Delta.Y == 0 ||
                e.KeyModifiers.HasFlag(KeyModifiers.Shift) ||  // If Shift is already pressed, let default behavior handle it
                FindScrollViewer(control) is not IScrollable scrollViewer )
            {
                return;
            }
            
            // Check if horizontal scrolling is possible
            var extent = scrollViewer.Extent.Width;
            var viewport = scrollViewer.Viewport.Width;
            
            if (extent <= viewport)
                return; // No horizontal scrolling needed

            // Redirect vertical scroll to horizontal
            var delta = e.Delta.Y;
            var scrollAmount = delta * 50; // Adjust multiplier for sensitivity
            
            scrollViewer.Offset = scrollViewer.Offset.WithX(
                Math.Clamp(scrollViewer.Offset.X - scrollAmount, 0, extent - viewport)
            );
            
            e.Handled = true;
        }

        private static IScrollable? FindScrollViewer(Control? control)
        {
            switch (control)
            {
                case null:
                    return null;
                case IScrollable scrollable:
                    return scrollable;
                case ListBox listBox:
                    return listBox.Scroll;
            }

            // Walk up the visual tree to find a scollable control
            var parent = control.Parent as Control;
            while (parent != null)
            {
                if (parent is IScrollable scrollable)
                    return scrollable;
                parent = parent.Parent as Control;
            }

            return null;
        }
    }
}