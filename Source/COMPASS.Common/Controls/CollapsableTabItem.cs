using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using COMPASS.Common.Models;
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
            AvaloniaProperty.Register<IconButton, MaterialIconKind>(nameof(Icon));

        /// <summary>
        /// Gets or sets the Icon property. This StyledProperty
        /// indicates what icon should be shown.
        /// </summary>
        public MaterialIconKind Icon
        {
            get => this.GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        private void TabItemClicked(object? sender, TappedEventArgs e)
        {
            var c = sender as Control;
            if (c!.DataContext is not IDealsWithTabControl vm) return;

            if (vm.PrevSelectedTab == TabIndex)
            {
                vm.SelectedTab = 0;
            }
            vm.PrevSelectedTab = TabIndex;
        }
    }
}
