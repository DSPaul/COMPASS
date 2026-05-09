using Avalonia.Controls;

namespace COMPASS.Common.Views.Windows;

public partial class LoadingWindow : Window
{
    public LoadingWindow() : this("Busy...")
    {}
    
    public LoadingWindow(string message)
    {
        Title = message;
        InitializeComponent();
    }
}