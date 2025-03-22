using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace COMPASS.Common.Views;

public partial class ColorPicker : UserControl
{
    public ColorPicker()
    {
        InitializeComponent();
        ColorGrid.ItemsSource = ColorOptions;
    }
    
    private Color? _selectedColor;

    public static readonly DirectProperty<ColorPicker, Color?> SelectedColorProperty = AvaloniaProperty.RegisterDirect<ColorPicker, Color?>(
        nameof(SelectedColor), o => o.SelectedColor, (o, v) => o.SelectedColor = v);

    public Color? SelectedColor
    {
        get => _selectedColor;
        set => SetAndRaise(SelectedColorProperty, ref _selectedColor, value);
    }

    public static readonly StyledProperty<IList<Color>> ColorOptionsProperty = AvaloniaProperty.Register<ColorPicker, IList<Color>>(
        nameof(ColorOptions),
        defaultValue: [
            Color.Parse("#FFB41717"),
            Color.Parse("#FFFF7700"),
            Color.Parse("#FFFA9000"),
            Color.Parse("#FFB87333"),
            Color.Parse("#FFC5BF0A"),
            Color.Parse("#FF83E219"),
            Color.Parse("#FF01FF1F"),
            Color.Parse("#FF2FAB1C"),
            Color.Parse("#FF13C38E"),
            Color.Parse("#FF00E7E7"),
            Color.Parse("#FF01CDFF"),
            Color.Parse("#FF2FA4D7"),
            Color.Parse("#FF188DCA"),
            Color.Parse("#FF015FFF"),
            Color.Parse("#FF6533E7"),
            Color.Parse("#FF9B50E8"),
            Color.Parse("#FFBE01FF"),
            Color.Parse("#FFFF01F7"),
            Color.Parse("#FFE5009C"),
            Color.Parse("#FFFF0141"),
        ]);

    public IList<Color> ColorOptions
    {
        get => GetValue(ColorOptionsProperty);
        set => SetValue(ColorOptionsProperty, value);
    }

    private void ToggleButton_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton radioButton && radioButton.IsChecked == true)
        {
            SelectedColor = radioButton.DataContext as Color?;
        }
    }
}