using Autofac;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using COMPASS.Common.Services;
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
                var crashNotification = CrashHandler.GetCrashNotification(crashMsg);
                MainWindow = new NotificationWindow(crashNotification);
                MainWindow.Closing += (s,e) => CrashHandler.OnCrashNotificationClosing(e, crashNotification, crashMsg);
                desktop.MainWindow = MainWindow;
            }
            else
            {
                // Open splash screen
                desktop.MainWindow = MainWindow =_splashScreenWindow = new SplashScreenWindow();
                
                // delegate actual application start to when UI thread has time again
                Dispatcher.UIThread.Post(() => CompleteApplicationStart(), DispatcherPriority.Background);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private SplashScreenWindow? _splashScreenWindow;
    
    public static Window MainWindow { get; private set; } = null!;

    private void CompleteApplicationStart()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            //Build container
            var mainWindow = new MainWindow();
            
            //load and set vm
            var mainVm =  new MainViewModel();
            mainWindow.DataContext = mainVm;

            desktop.MainWindow = mainWindow;

            // Show main window to avoid framework shutdown when closing splash screen
            mainWindow.Show();
            
            //must be done after window is shown, as notification service will use it as parent
            //and showing a notification on a non visible window causes a crash
            MainWindow = mainWindow; 

            // Finally, close the splash screen
            _splashScreenWindow?.Close();
        }
    }
}
