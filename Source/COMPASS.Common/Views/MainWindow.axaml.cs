using Avalonia.Controls;

namespace COMPASS.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendClientAreaToDecorationsHint = true; //allows me to put stuff in title bar
    }
}
