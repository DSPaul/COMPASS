using Avalonia.Controls;
using COMPASS.Common.Services;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels;
using COMPASS.Common.ViewModels.Main;

namespace COMPASS.Common.Views.Windows;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendClientAreaToDecorationsHint = true;
    }

    private void Window_Closing(object? sender, Avalonia.Controls.WindowClosingEventArgs e)
    {
        ProgressViewModel.GetInstance().CancelBackgroundTask();
        if (MainViewModel.SaveOnClose)
        {
            CollectionManager.SaveAllCollections();
            PreferencesService.GetInstance().SavePreferences();
        }
    }
}
