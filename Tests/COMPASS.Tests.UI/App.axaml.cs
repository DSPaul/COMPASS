using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels.Main;

namespace COMPASS.Tests.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = WindowManager.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}