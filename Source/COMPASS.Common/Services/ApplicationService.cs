using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Infra.Interfaces.Services;
using COMPASS.Infra.Models;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.Services;

public static class ApplicationService
{
    public static string Version { get; } = GetVersion();

    public static string GetVersion()
    {
        return Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "Unknown";
    }
    
    public static void Shutdown()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
        {
            desktopApp.Shutdown();
        }
    }

    public static void Restart(bool keepArgs)
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
}