using Avalonia.Controls;
using COMPASS.Common.ViewModels;

namespace COMPASS.Common.Views.Windows;

public partial class ChangeDataLocationWindow : Window
{
    public ChangeDataLocationWindow(SettingsViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}