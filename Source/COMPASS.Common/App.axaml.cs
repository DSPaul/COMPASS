using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using COMPASS.Common.EventHandlers;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Services;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.Views.Windows;
using COMPASS.Infra.Tools;
using NuGet.Versioning;

namespace COMPASS.Common;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        MarkdownViewerLinkHandler.EnsureRegistered();
        
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
            HandleVersionChanges();

            var mainWindow = new MainWindow();
            var mainVm = new MainViewModel();
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

    /// <summary>
    /// Handle launching a different version of the application
    /// </summary>
    private static void HandleVersionChanges()
    {
        var preferencesService = ServiceResolver.Resolve<IPreferencesService>();
        var logger = ServiceResolver.Resolve<ILogger>();

        SemanticVersion? lastRanVersion = preferencesService.Preferences.LastRanVersion;
        SemanticVersion? currentVersion = SemanticVersion.Parse(ApplicationService.Version);

        //Check if magration from v1 is needed
        if (lastRanVersion == null || lastRanVersion.Major == 1)
        {
            ServiceResolver.Resolve<IApplicationDataService>().MigrateFromV1();
        }

        //Check if current verion is newer or older than last ran version
        if (lastRanVersion == null || lastRanVersion < currentVersion)
        {
            ApplicationService.FirstRunSinceUpdate = true;
        }
        else if(lastRanVersion > currentVersion)
        {
            logger.Debug($"Downgrade detected, {lastRanVersion} -> {currentVersion}");
        }

        preferencesService.Preferences.LastRanVersion = currentVersion;
        preferencesService.SavePreferences();
    }
}