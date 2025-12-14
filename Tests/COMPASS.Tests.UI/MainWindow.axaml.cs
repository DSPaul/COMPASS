using Avalonia.Controls;
using COMPASS.Common.Views.Main;
using COMPASS.Tests.UI.ViewModels;

namespace COMPASS.Tests.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        DataContext = new MainViewModel();
        InitializeComponent();
    }
}