using Avalonia.Controls;

namespace COMPASS.Common.Views.Windows;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendClientAreaToDecorationsHint = true; //allows me to put stuff in title bar
    }
}
