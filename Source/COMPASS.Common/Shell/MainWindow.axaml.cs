using Avalonia;
using Avalonia.Controls;
using COMPASS.Common.Features.CodexCollections;
using COMPASS.Common.Features.Settings;
using COMPASS.Infra.DependencyInjection;
using COMPASS.Infra.Preferences;

namespace COMPASS.Common.Shell;

public partial class MainWindow : Window
{
    private IPreferencesService _preferencesService;

    public MainWindow()
    {
        _preferencesService = ServiceResolver.Resolve<IPreferencesService>();

        InitializeComponent();
        ExtendClientAreaToDecorationsHint = true;
        RestoreWindowPlacement();
    }

    private void RestoreWindowPlacement()
    {
        var windowState = _preferencesService.GetPreferences<WindowRestoreState>();

        Width = windowState.Width;
        Height = windowState.Height;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Position = new PixelPoint(windowState.X, windowState.Y);
        WindowState = windowState.WindowState;
    }

    private void UpdateWindowPlacement()
    {
        var windowState = _preferencesService.GetPreferences<WindowRestoreState>();
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
