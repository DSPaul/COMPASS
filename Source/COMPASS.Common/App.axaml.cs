using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Services;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.Views.Windows;
using COMPASS.Infra.Tools;

namespace COMPASS.Common;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            //if crash, show crash dialog instead
            string? crashMsg = CmdLineArgumentService.Args?.CrashMessage;
            if (!string.IsNullOrEmpty(crashMsg))
            {
                var crashNotification = CrashHandler.GetCrashNotification(crashMsg);
                var crashWindow = new NotificationWindow(crashNotification);
                crashWindow.Closing += (s,e) => CrashHandler.OnCrashNotificationClosing(e, crashNotification, crashMsg);
                desktop.MainWindow = WindowManager.MainWindow = crashWindow;
            }
            else
            {
                // Open splash screen
                desktop.MainWindow = WindowManager.MainWindow =_splashScreenWindow = new SplashScreenWindow();
                
                // delegate actual application start to when UI thread has time again
                Dispatcher.UIThread.Post(CompleteApplicationStart, DispatcherPriority.Background);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private SplashScreenWindow? _splashScreenWindow;

    private void CompleteApplicationStart()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            ServiceResolver.Resolve<IApplicationDataService>().MigrateFromV1();

            var mainWindow = new MainWindow();
            var mainVm =  new MainViewModel();
            mainWindow.DataContext = mainVm;

            desktop.MainWindow = mainWindow;

            // Show main window to avoid framework shutdown when closing splash screen
            mainWindow.Show();
            
            //must be done after window is shown, as notification service will use it as parent
            //and showing a notification on a non visible window causes a crash
            WindowManager.MainWindow = mainWindow;
            ConnectivityManager.SubscribeToWindowFocus(mainWindow);

            // Finally, close the splash screen
            _splashScreenWindow?.Close();
        }
    }
}