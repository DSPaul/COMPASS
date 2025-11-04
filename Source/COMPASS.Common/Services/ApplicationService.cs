using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace COMPASS.Common.Services;

public static class ApplicationService
{
    public static string Version { get; } = GetVersion();

    public static string GetVersion()
    {
        try
        {
            //TODO Make this work on Linux
            string? assemblyName = Process.GetCurrentProcess().MainModule?.FileName;
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assemblyName!);
            return fvi.FileVersion![..5];
        }
        catch
        {
            return "2.0.0";
        }
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
        var args = keepArgs ? Environment.GetCommandLineArgs() : [];
        if (currentExecutablePath != null) Process.Start(currentExecutablePath, args);
        Shutdown();
    }
}