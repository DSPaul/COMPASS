using Avalonia;
using Avalonia.Controls;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.Views.Windows;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendClientAreaToDecorationsHint = true;
        RestoreWindowPlacement();
    }

    private void RestoreWindowPlacement()
    {
        var windowState = ServiceResolver.Resolve<IPreferencesService>().Preferences.WindowState;

        Width = windowState.Width;
        Height = windowState.Height;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Position = new PixelPoint(windowState.X, windowState.Y);
        WindowState = windowState.WindowState;
    }

    private void UpdateWindowPlacement()
    {
        var windowState = ServiceResolver.Resolve<IPreferencesService>().Preferences.WindowState;
        windowState.WindowState = WindowState == WindowState.Maximized ? WindowState.Maximized : WindowState.Normal;
        if (WindowState == WindowState.Normal)
        {
            windowState.Width = Width;
            windowState.Height = Height;
            windowState.X = Position.X;
            windowState.Y = Position.Y;
        }
    }

    private void Window_Closing(object? sender, Avalonia.Controls.WindowClosingEventArgs e)
    {
        if (MainViewModel.SaveOnClose)
        {
            UpdateWindowPlacement();
            ServiceResolver.Resolve<CollectionManager>().SaveAllCollections();
            ServiceResolver.Resolve<IPreferencesService>().SavePreferences();
        }
    }
}
