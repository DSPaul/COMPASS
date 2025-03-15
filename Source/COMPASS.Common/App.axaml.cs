using System;
using System.Threading.Tasks;
using Autofac;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Services;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;
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
            //if crash, show crash dialog instead
            string? crashMsg = CmdLineArgumentService.Args?.CrashMessage;
            if (!string.IsNullOrEmpty(crashMsg))
            {
                Container = ContainerBuilder!.Build();
                var crashNotification = CrashHandler.GetCrashNotification(crashMsg);
                MainWindow = new NotificationWindow(crashNotification);
                MainWindow.Closing += (s,e) => CrashHandler.OnCrashNotificationClosing(e, crashNotification, crashMsg);
                desktop.MainWindow = MainWindow;
            }
            else
            {
                // Open splash screen
                _splashScreenWindow = new SplashScreenWindow();
                desktop.MainWindow = _splashScreenWindow;
                
                // delegate actual application start to when UI thread has time again
                Dispatcher.UIThread.Post(() => CompleteApplicationStart(), DispatcherPriority.Background);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private SplashScreenWindow? _splashScreenWindow;
    
    public static Window MainWindow { get; private set; }

    public static ContainerBuilder? ContainerBuilder { get; set; }
    public static IContainer Container { get; set; }

    private IContainer BuildContainer(Window window)
    {
        if(ContainerBuilder == null) throw new InvalidOperationException("Container builder should be constructed by platform specific project");
        
        ContainerBuilder.RegisterInstance<IFilesService>(new FilesService(window));
        return ContainerBuilder.Build();
    }
    
    private void CompleteApplicationStart()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            //Build container
            MainWindow = new MainWindow();
            Container = BuildContainer(MainWindow);
            
            //load and set vm
            var mainVm =  new MainViewModel();
            MainWindow.DataContext = mainVm;

            desktop.MainWindow = MainWindow;

            // Show main window to avoid framework shutdown when closing splash screen
            MainWindow.Show();

            // Finally, close the splash screen
            _splashScreenWindow?.Close();
        }
    }
}
