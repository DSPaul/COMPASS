using Avalonia;
using Avalonia.Controls;
using Material.Icons;

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

    public static readonly StyledProperty<string?> CancelLabelProperty = AvaloniaProperty.Register<ConfirmControls, string?>(
        nameof(CancelLabel), "Cancel");

    public string? CancelLabel
    {
        get => GetValue(CancelLabelProperty);
        set => SetValue(CancelLabelProperty, value);
    }

    public static readonly StyledProperty<MaterialIconKind?> CancelIconProperty = AvaloniaProperty.Register<ConfirmControls, MaterialIconKind?>(
        nameof(CancelIcon));

    public MaterialIconKind? CancelIcon
    {
        get => GetValue(CancelIconProperty);
        set => SetValue(CancelIconProperty, value);
    }

    public static readonly StyledProperty<string?> ConfirmLabelProperty = AvaloniaProperty.Register<ConfirmControls, string?>(
        nameof(ConfirmLabel), "Ok");

    public string? ConfirmLabel
    {
        get => GetValue(ConfirmLabelProperty);
        set => SetValue(ConfirmLabelProperty, value);
    }

    public static readonly StyledProperty<MaterialIconKind?> ConfirmIconProperty = AvaloniaProperty.Register<ConfirmControls, MaterialIconKind?>(
        nameof(ConfirmIcon), defaultValue: MaterialIconKind.Check);

    public MaterialIconKind? ConfirmIcon
    {
        get => GetValue(ConfirmIconProperty);
        set => SetValue(ConfirmIconProperty, value);
    }
    
    // We override OnPropertyChanged of the base class. That way we can react on property changes
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // if the changed property is the NumberOfStarsProperty, we need to update the stars
        if (change.Property == CompactProperty)
        {
            if (Compact)
            {
                ConfirmLabel = null;
                CancelLabel = null;
            }
        }
    }

}