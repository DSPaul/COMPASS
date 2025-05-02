using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Autofac;
using Avalonia.Controls;
using Avalonia.Threading;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Models;
using COMPASS.Common.Models.ApiDtos;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services;

namespace COMPASS.Common.Tools;

public static class CrashHandler
{
    const string RestartOption = "Restart COMPASS.";
    const string SubmitOption = "Submit an anonymous crash report.";

    private static bool _crashHandled = false;
    
    public static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            Logger.Fatal("Unhandled non UI exception", exception);
            HandleCrash(exception);
        }
    }

    public static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Logger.Fatal("Unobserved Task exception", e.Exception);
        HandleCrash(e.Exception);
        e.SetObserved(); // Prevent the process from terminating
    }

    public static void HandleCrash(Exception ex)
    {
        //something crashed when already handling a crash, just give up at this point
        if (!string.IsNullOrEmpty(CmdLineArgumentService.Args?.CrashMessage))
        {
            Logger.Fatal("Crash during crash notification", ex);
            Environment.Exit(1);
        }
        
        //Restart app with exception as argument
        var currentExecutablePath = Environment.ProcessPath;
        string[] args = [$"--{Constants.CmdArgNotifyCrashed}", $"\"{ex}\""];
        if (currentExecutablePath != null) Process.Start(currentExecutablePath, args);
        
        Environment.Exit(1);
    }

    public static Notification GetCrashNotification(string exceptionMessage)
    {
        //prompt user to submit logs and open an issue
        string message = $"An unexpected error ocurred.\n \n" +
                         $"You can help improve COMPASS by reporting the issue on either discord, reddit, the github repo or by filling in an anonymous Google form. \n \n" +
                         $"Links to all of these can be found at {Constants.LinkTreeURL}. \n \n" +
                         $"Please include the log file located at {ServiceResolver.Resolve<IEnvironmentVarsService>().CompassDataPath}\\logs";

        Notification crashNotification = new($"COMPASS ran into a critical error.", message, Severity.Error);
        crashNotification.Details = exceptionMessage;
        
        crashNotification.Options.Add(new(RestartOption, true));
        crashNotification.Options.Add(new(SubmitOption, true));

        return crashNotification;
    }

    public static void OnCrashNotificationClosing(WindowClosingEventArgs e, Notification crashNotification, string exceptionMessage)
    {
        if (!_crashHandled)
        {
            // Cancel the close
            e.Cancel = true;
            
            // Set a flag to avoid infinite recursion
            _crashHandled = true;
            _ = HandleCrashNotification(crashNotification, exceptionMessage);
        }
    }
    
    private static async Task HandleCrashNotification(Notification crashNotification, string exceptionMessage)
    {
        if (crashNotification.IsOptionSelected(SubmitOption))
        {
            var report = new CrashReport(exceptionMessage);
            using HttpResponseMessage result = await new ApiClientService().PostAsync(report, "submit/crash");
            Logger.Debug(result.ToString());
        }

        //Restart
        if (crashNotification.IsOptionSelected(RestartOption))
        {
            var currentExecutablePath = Environment.ProcessPath;
            if (currentExecutablePath != null) Process.Start(currentExecutablePath);
        }
        
        //Close window from main thread
        await Dispatcher.UIThread.InvokeAsync(() => 
        {
            App.MainWindow.Close();
        });
    }
}