using Avalonia.Controls;
using COMPASS.Common.ViewModels;

namespace COMPASS.Common.Views.Windows;

public partial class FileNotFoundWindow : Window
{
    public FileNotFoundWindow(FileNotFoundViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}