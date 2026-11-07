using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using COMPASS.Common.Features.Settings;
using COMPASS.Common.Shell;
using COMPASS.Infra.Application;
using COMPASS.Infra.Avalonia.Markdown;
using COMPASS.Infra.Avalonia.Modal;
using COMPASS.Infra.DependencyInjection;
using COMPASS.Infra.IO;
using COMPASS.Infra.Logging;
using COMPASS.Infra.Preferences;
using COMPASS.Infra.Progress;
using NuGet.Versioning;
using COMPASS.Common.Features.Notifications;
using COMPASS.Common.Features.Onboarding;

namespace COMPASS.Common;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        MarkdownViewerLinkHandler.EnsureRegistered();
        ViewRegistrar.RegisterModals();
        ViewRegistrar.RegisterLayouts();
        
#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        //Capture the UI thread's context so background
        //can marshal notifications back to it from any thread.
        UiSynchronizationContext.Initialize(SynchronizationContext.Current
            ?? throw new InvalidOperationException("No SynchronizationContext on UI thread"));

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            //Dispose the container on shutdown, which flushes disposable singletons
            //like the log pipeline; without this the last log events may never reach the file
            desktop.Exit += static (_, _) =>
            {
                try
                {
                    ServiceResolver.Dispose();
                }
                catch (Exception)
                {
                    //Shutting down; nothing left to do
                }
            };

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
            int lastRunVersion = HandleVersionChanges();

            var mainWindow = new MainWindow();
            var mainVm = ServiceResolver.Resolve<MainViewModel>();
            mainWindow.DataContext = mainVm;

            desktop.MainWindow = mainWindow;

            // Show main window to avoid framework shutdown when closing splash screen
            mainWindow.Show();

            //must be done after window is shown, as notification service will use it as parent
            //and showing a notification on a non visible window causes a crash
            WindowManager.MainWindow = mainWindow;

            // Finally, close the splash screen
            _splashScreenWindow?.Close();

            if (lastRunVersion == 1)
            {
                Dispatcher.UIThread.Post(() => _ = WindowManager.OpenModal(new V1toV2OnboardingVM()), DispatcherPriority.Background);
            }
        }
    }

    /// <summary>
    /// Handle launching a different version of the application
    /// </summary>
    /// <returns>The last run version</returns>
    private static int HandleVersionChanges()
    {
        var preferencesService = ServiceResolver.Resolve<IPreferencesService>();
        var applicationService = ServiceResolver.Resolve<IApplicationService>();
        var applicationDataService = ServiceResolver.Resolve<IApplicationDataService>();
        var logger = ServiceResolver.Resolve<ILogger>();


        SemanticVersion? lastRanVersion = preferencesService.GetPreferences<Preferences>().LastRanVersion;
        SemanticVersion? currentVersion = SemanticVersion.Parse(applicationService.Version);
        
        if (lastRanVersion == null || lastRanVersion.Major == 1)
        {
            applicationDataService.MigrateFromV1();
        }

        //We check this after datamigration, as detecting the user data location has changed between v1 and v2
        //So checking for user data first would use the new logic on old data and detect nothing
        bool freshInstall = lastRanVersion == null && !applicationDataService.HasExistingUserData();

        //Check if current verion is newer or older than last ran version
        if (lastRanVersion == null || lastRanVersion < currentVersion)
        {
            applicationService.FirstRunSinceUpdate = true;
        }
        else if(lastRanVersion > currentVersion)
        {
            logger.Debug($"Downgrade detected, {lastRanVersion} -> {currentVersion}");
        }

        preferencesService.GetPreferences<Preferences>().LastRanVersion = currentVersion;
        preferencesService.SavePreferences();

        return freshInstall ? 0 : lastRanVersion?.Major ?? 1;
    }
}
