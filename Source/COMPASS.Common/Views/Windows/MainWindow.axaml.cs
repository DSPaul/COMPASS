using Avalonia.Controls;
using COMPASS.Common.Services;
using COMPASS.Common.ViewModels;

namespace COMPASS.Common.Views.Windows;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendClientAreaToDecorationsHint = true; //allows me to put stuff in title bar
    }

    private void Window_Closing(object? sender, Avalonia.Controls.WindowClosingEventArgs e)
    {
        ProgressViewModel.GetInstance().CancelBackgroundTask();
        MainViewModel.CollectionVM.CurrentCollection.Save();
        PreferencesService.GetInstance().SavePreferences();
    }
}
