using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using COMPASS.Infra.Models.Enums;

namespace COMPASS.Common.Controls
{
    public class CollapsableTabItem : ListBoxItem
    {
        public CollapsableTabItem()
        {
            // The ListBox updates its selection during the bubbling phase of PointerPressed.
            // Reading IsSelected in a tunneling handler therefore reports whether this tab was
            // already open before this click, which decides between collapsing and opening.
            AddHandler(PointerPressedEvent, TabHeaderPressed, RoutingStrategies.Tunnel);
        }

        public static readonly StyledProperty<object?> HeaderProperty = AvaloniaProperty.Register<CollapsableTabItem, object?>(
        nameof(Header));

        public object? Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public static readonly StyledProperty<object?> IconProperty = AvaloniaProperty.Register<CollapsableTabItem, object?>(
        nameof(Icon));

        public object? Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public static readonly StyledProperty<bool> HideHeaderProperty = AvaloniaProperty.Register<CollapsableTabItem, bool>(
        nameof(HideHeader));

        public bool HideHeader
        {
            get => GetValue(HideHeaderProperty);
            set => SetValue(HideHeaderProperty, value);
        }

        public static readonly StyledProperty<Severity> AttentionSeverityProperty =
            AvaloniaProperty.Register<CollapsableTabItem, Severity>(nameof(AttentionSeverity));

        public Severity AttentionSeverity
        {
            get => GetValue(AttentionSeverityProperty);
            set => SetValue(AttentionSeverityProperty, value);
        }

        public static readonly StyledProperty<bool> ShowAttentionProperty =
            AvaloniaProperty.Register<CollapsableTabItem, bool>(nameof(ShowAttention));

        public bool ShowAttention
        {
            get => GetValue(ShowAttentionProperty);
            set => SetValue(ShowAttentionProperty, value);
        }

        protected void TabHeaderPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

            AttentionSeverity = Severity.Info;
            ShowAttention = false;

            //Click open tab -> collapse; marking the event handled stops the ListBox from re-selecting it
            if (IsSelected && ItemsControl.ItemsControlFromItemContainer(this) is SelectingItemsControl selecting)
            {
                selecting.SelectedIndex = -1;
                e.Handled = true;
            }
        }
    }
}
