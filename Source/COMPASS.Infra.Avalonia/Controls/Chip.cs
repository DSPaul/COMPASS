using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace COMPASS.Infra.Avalonia.Controls;

public class Chip : TemplatedControl
{
    public static readonly StyledProperty<string?> TextProperty = TextBlock.TextProperty.AddOwner<Chip>();

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}