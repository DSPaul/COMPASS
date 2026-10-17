using Avalonia.Controls.ApplicationLifetimes;
using COMPASS.Infra.Application;
using COMPASS.Infra.DependencyInjection;
using COMPASS.Infra.Logging;
using COMPASS.Infra.Notifications;
using System.Diagnostics;
using System.Reflection;

namespace COMPASS.Infra.Avalonia.Application;

internal class ApplicationService : IApplicationService
{
    public string Version => field ??= GetVersion();

    public bool FirstRunSinceUpdate { get; set; } = false;
    
    public void Shutdown()
    {
        if (global::Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
        {
            desktopApp.Shutdown();
        }
    }

    public void Restart(bool keepArgs)
    {
        var currentExecutablePath = Environment.ProcessPath;
        if (currentExecutablePath == null)
        {
            ServiceResolver.Resolve<ILogger>().Debug("Failed to restart application: unable to determine executable path.");
            var notification = new Notification("COMPASS required a restart", "COMPASS failed to automatically restart. Please restart the application manually.");
            ServiceResolver.Resolve<INotificationService>().Notify(notification);
            return;
        }
        
        var args = keepArgs ? Environment.GetCommandLineArgs().Skip(1) : []; //first arg is execution path
        Process.Start(currentExecutablePath, args);
        Shutdown();
    }

    private static string GetVersion()
    {
        return Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "Unknown";
    }
}