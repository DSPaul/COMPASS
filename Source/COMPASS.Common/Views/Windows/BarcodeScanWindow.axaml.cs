using Avalonia.Controls;

namespace COMPASS.Common;

public partial class BarcodeScanWindow : Window
{
    public BarcodeScanWindow()
    {
        InitializeComponent();
    }

    public string? DecodedString { get; set; }
}