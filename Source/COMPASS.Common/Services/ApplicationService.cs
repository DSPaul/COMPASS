using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

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
            //Doubt this ever happens, if it does, tell user they must manually restart the app
            //TODO
            return;
        }
        
        var args = keepArgs ? Environment.GetCommandLineArgs().Skip(1) : []; //first arg is execution path
        Process.Start(currentExecutablePath, args);
        Shutdown();
    }
}