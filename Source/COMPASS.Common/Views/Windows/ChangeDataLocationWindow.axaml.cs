using Avalonia.Controls;
using COMPASS.Common.ViewModels;

namespace COMPASS.Common;

public partial class ChangeDataLocationWindow : Window
{
    public ChangeDataLocationWindow(SettingsViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}