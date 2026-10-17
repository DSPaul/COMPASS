using Avalonia.Controls;

namespace COMPASS.Common.Shell;

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