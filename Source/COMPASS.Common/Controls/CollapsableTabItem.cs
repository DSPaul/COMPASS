using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using COMPASS.Common.Models;
using COMPASS.Infra.Models.Enums;

namespace COMPASS.Common.Controls
{
    public class CollapsableTabItem : TabItem
    {
        public CollapsableTabItem()
        {
            AddHandler(TappedEvent, TabItemClicked);
            AddHandler(DoubleTappedEvent, TabItemClicked);
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

        protected void TabItemClicked(object? sender, TappedEventArgs e)
        {
            AttentionSeverity = Severity.Info;
            ShowAttention = false;

            if (DataContext is not IDealsWithTabControl vm) return;
            
            //Click open tab -> collapse
            if (vm.PrevSelectedTab == TabIndex)
            {
                vm.SelectedTab = 0;
            }
            
            vm.PrevSelectedTab = TabIndex;
        }
    }
}
