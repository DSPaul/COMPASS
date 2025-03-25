using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace COMPASS.Common.Controls;

public partial class ConfirmControls : UserControl
{
    public ConfirmControls()
    {
        InitializeComponent();
    }

    public static readonly StyledProperty<bool> CompactProperty = AvaloniaProperty.Register<ConfirmControls, bool>(
        nameof(Compact));

    public bool Compact
    {
        get => GetValue(CompactProperty);
        set => SetValue(CompactProperty, value);
    }

    public static readonly StyledProperty<string> CancelLabelProperty = AvaloniaProperty.Register<ConfirmControls, string>(
        nameof(CancelLabel), "Cancel");

    public string CancelLabel
    {
        get => Compact ? string.Empty : GetValue(CancelLabelProperty);
        set => SetValue(CancelLabelProperty, value);
    }

    public static readonly StyledProperty<string> ConfirmLabelProperty = AvaloniaProperty.Register<ConfirmControls, string>(
        nameof(ConfirmLabel), "Ok");

    public string ConfirmLabel
    {
        get => Compact ? string.Empty : GetValue(ConfirmLabelProperty);
        set => SetValue(ConfirmLabelProperty, value);
    }
}