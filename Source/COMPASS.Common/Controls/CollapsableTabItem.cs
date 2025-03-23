using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using Material.Icons;

namespace COMPASS.Common.Controls
{
    public class CollapsableTabItem : TabItem
    {
        public CollapsableTabItem()
        {
            AddHandler(Gestures.TappedEvent, TabItemClicked);
            AddHandler(Gestures.DoubleTappedEvent, TabItemClicked);
        }

        /// <summary>
        /// Icon StyledProperty definition
        /// </summary>
        public static readonly StyledProperty<MaterialIconKind> IconProperty =
            AvaloniaProperty.Register<CollapsableTabItem, MaterialIconKind>(nameof(Icon));
        
        public MaterialIconKind Icon
        {
            get => this.GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }
        
        /// <summary>
        /// AttentionSeverity StyledProperty definition
        /// </summary>
        public static readonly StyledProperty<Severity> AttentionSeverityProperty =
            AvaloniaProperty.Register<CollapsableTabItem, Severity>(nameof(AttentionSeverity));
        
        public Severity AttentionSeverity
        {
            get => this.GetValue(AttentionSeverityProperty);
            set => SetValue(AttentionSeverityProperty, value);
        }

        /// <summary>
        /// ShowAttention StyledProperty definition
        /// </summary>
        public static readonly StyledProperty<bool> ShowAttentionProperty =
            AvaloniaProperty.Register<CollapsableTabItem, bool>(nameof(ShowAttention));
        
        public bool ShowAttention
        {
            get => this.GetValue(ShowAttentionProperty);
            set => SetValue(ShowAttentionProperty, value);
        }

        private void TabItemClicked(object? sender, TappedEventArgs e)
        {
            if (DataContext is not IDealsWithTabControl vm) return;
            
            //Click open tab -> collapse
            if (vm.PrevSelectedTab == TabIndex)
            {
                vm.SelectedTab = 0;
            }
            //Switch from another tab to this one, hide attention
            else
            {
                ShowAttention = false;
            }
            vm.PrevSelectedTab = TabIndex;
        }
    }
}
