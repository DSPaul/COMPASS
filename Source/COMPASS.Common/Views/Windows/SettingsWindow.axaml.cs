using Avalonia.Controls;
using COMPASS.Common.ViewModels;

namespace COMPASS.Common;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel vm, string tab)
    {
        InitializeComponent();
        DataContext = vm;
    }
}