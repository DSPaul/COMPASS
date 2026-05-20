using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using COMPASS.ApiClients.Compass;
using COMPASS.ApiClients.Compass.Models;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Services;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Enums;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.Tools;

public static class CrashHandler
{
    private static ILogger? _logger;
    private static ILogger Logger => _logger ??= ServiceResolver.Resolve<ILogger>();

    const string RestartLabel = "Restart COMPASS.";
    const string SubmitLabel = "Submit an anonymous crash report.";

    const string RestartOptionId = "RESTART";
    const string SubmitOptionId = "SUBMIT";

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
        if (e.Exception.InnerExceptions.All(IsIgnorableException))
        {
            Logger.Debug("Suppressed unobserved task exception", e.Exception);
            e.SetObserved();
            return;
        }

        Logger.Fatal("Unobserved Task exception", e.Exception);
        HandleCrash(e.Exception);
        e.SetObserved(); // Prevent the process from terminating
    }

    private static bool IsIgnorableException(Exception ex) => ex switch
    {
        // Avalonia's DBusPlatformSettings fire-and-forgets WatchSettingChangedAsync without
        // exception handling. On Linux systems without xdg-desktop-portal installed,
        // org.freedesktop.portal.Desktop is not activatable and this surfaces as an unobserved
        // task exception when the GC finalizer runs. Known Avalonia bug; suppress it.
        { } e when e.GetType().FullName == "Tmds.DBus.Protocol.DBusException" => true,
        AggregateException agg => agg.InnerExceptions.All(IsIgnorableException),
        _ => false
    };

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
                         $"Please include the log file located at {IApplicationDataService.ApplicationDataPath}{Path.DirectorySeparatorChar}logs";

        Notification crashNotification = new($"COMPASS ran into a critical error.", message, Severity.Error);
        crashNotification.Details = exceptionMessage;
        
        crashNotification.Options.Add(new(RestartOptionId, RestartLabel, true));
        crashNotification.Options.Add(new(SubmitOptionId, SubmitLabel, true));

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
        if (crashNotification.IsOptionSelected(SubmitOptionId))
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
                WindowManager.MainWindow.Cursor = new Cursor(StandardCursorType.Wait));
            try
            {
                var request = new CrashReport
                {
                    Version = ApplicationService.Version,
                    OperatingSystem = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                    Error = exceptionMessage
                };
                var compassApiClient = ServiceResolver.Resolve<ICompassApiClient>();
                await compassApiClient.SubmitCrashReportAsync(request);
                Logger.Debug("Crash report submitted successfully");
            }
            catch(HttpRequestException httpEx)
            {
                Logger.Error("Failed to submit crash report due to HTTP error", httpEx);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to submit crash report", ex);
            }
            finally
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                    WindowManager.MainWindow.Cursor = Cursor.Default);
            }
        }

        //Restart
        if (crashNotification.IsOptionSelected(RestartOptionId))
        {
            var currentExecutablePath = Environment.ProcessPath;
            if (currentExecutablePath != null) Process.Start(currentExecutablePath);
        }
        
        //Close window from main thread
        await Dispatcher.UIThread.InvokeAsync(() => 
        {
            WindowManager.MainWindow.Close();
        });
    }
}