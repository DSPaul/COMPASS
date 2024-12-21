using Avalonia.Controls;

namespace COMPASS.Common.Views.Windows;

public partial class BarcodeScanWindow : Window
{
    public BarcodeScanWindow()
    {
        InitializeComponent();
    }

    public string? DecodedString { get; set; }
}