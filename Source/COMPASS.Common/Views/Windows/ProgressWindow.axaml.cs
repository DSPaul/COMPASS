using Avalonia.Controls;
using COMPASS.Common.ViewModels;

namespace COMPASS.Common.Views.Windows;

public partial class ProgressWindow : Window
{
    public ProgressWindow(int bars = 1)
    {
        InitializeComponent();
        DataContext = ProgressViewModel.GetInstance();
    }
}