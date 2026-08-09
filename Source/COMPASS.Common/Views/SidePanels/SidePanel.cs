using Avalonia;
using Avalonia.Controls;

namespace COMPASS.Common.Views.SidePanels
{
    public class SidePanel : UserControl
    {
        public static readonly StyledProperty<object?> HeaderProperty = AvaloniaProperty.Register<SidePanel, object?>(
        nameof(Header));

        public object? Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public static readonly StyledProperty<object?> IconProperty = AvaloniaProperty.Register<SidePanel, object?>(
        nameof(Icon));

        public object? Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }
    }
}
