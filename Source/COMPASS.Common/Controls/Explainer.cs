using Avalonia;
using Avalonia.Controls;
using Material.Icons;

namespace COMPASS.Common.Controls;

public class Explainer : ContentControl
{
    /// <summary>
    /// Icon StyledProperty definition
    /// </summary>
    public static readonly StyledProperty<MaterialIconKind> IconProperty =
        AvaloniaProperty.Register<IconButton, MaterialIconKind>(nameof(Icon), MaterialIconKind.AboutCircleOutline);

    /// <summary>
    /// Gets or sets the Icon property. This StyledProperty
    /// indicates what icon should be shown.
    /// </summary>
    public MaterialIconKind Icon
    {
        get => this.GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
}