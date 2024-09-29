using Autofac;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.ViewModels;
using COMPASS.Common.Views.Windows;

namespace COMPASS.Common;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();

            Container = BuildContainer(desktop.MainWindow);

            desktop.MainWindow.DataContext = new MainViewModel();
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static IContainer Container { get; set; }

    private IContainer BuildContainer(Window window)
    {
        //init the container
        var builder = new ContainerBuilder();

        builder.RegisterType<WindowedNotificationService>().Keyed<INotificationService>(NotificationDisplayType.Windowed);
        builder.RegisterType<WindowedNotificationService>().Keyed<INotificationService>(NotificationDisplayType.Toast); //use windowed for everything for now

        builder.RegisterInstance<IFilesService>(new FilesService(window));

        return builder.Build();
    }
}
