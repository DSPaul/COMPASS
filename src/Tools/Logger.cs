using Autofac;
using COMPASS.Interfaces;
using COMPASS.Models;
using COMPASS.Models.ApiDtos;
using COMPASS.Models.Enums;
using COMPASS.Services;
using COMPASS.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows;

namespace COMPASS.Tools
{
    public static class Logger
    {
        public static void Init()
        {
            log4net.GlobalContext.Properties["CompassDataPath"] = SettingsViewModel.CompassDataPath;
            log4net.Config.XmlConfigurator.Configure(new FileInfo("log4net.config"));
            FileLog = log4net.LogManager.GetLogger(nameof(Logger));
            if (Application.Current is not null)
            {
                Application.Current.DispatcherUnhandledException += LogUnhandledException;
            }
            Info($"Launching Compass v{Reflection.Version}");
        }

        // Log To file
        public static log4net.ILog? FileLog { get; private set; }

        // Log to Log Tab
        public static ObservableCollection<LogEntry> ActivityLog { get; } = new ObservableCollection<LogEntry>();

        public static void Info(string message) =>
            App.SafeDispatcher.Invoke(()
                => ActivityLog.Add(new(Severity.Info, message)));

        public static void Debug(string message) => FileLog?.Debug(message);


        public static void Warn(string message, Exception? ex = null)
        {
            App.SafeDispatcher.Invoke(() => ActivityLog.Add(new(Severity.Warning, message)));
            if (ex is null)
            {
                FileLog?.Warn(message);
            }
            else
            {
                FileLog?.Warn(message, ex);
            }
        }

        public static void Error(string message, Exception ex)
        {
            App.SafeDispatcher.Invoke(() => ActivityLog.Add(new(Severity.Error, message)));
            FileLog?.Error(message, ex);
        }

        private static void LogUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            FileLog?.Fatal(e.Exception.ToString(), e.Exception);

            //prompt user to submit logs and open an issue
            string message = $"An unexpected error ocurred.\n" +
                $"{e.Exception.Message}" +
                $"You can help improve COMPASS by opening an issue on {Constants.RepoURL} with the error message. \n" +
                $"Please include the log file located at {SettingsViewModel.CompassDataPath}\\logs \n." +
                $"COMPASS will now restart.";

            Notification crashNotification = new($"COMPASS ran into a critical error.", message, Severity.Error);
            string submitOption = "Submit an anonymous crash report.";
            crashNotification.Options.Add(new(submitOption, true));

            App.Container.ResolveKeyed<INotificationService>(NotificationDisplayType.Windowed).Show(crashNotification);

            if (crashNotification.IsOptionSelected(submitOption))
            {
                var report = new CrashReport(e.Exception);
                var result = new ApiClientService().PostAsync(report, "submit/crash").Result;
                Info(result.ToString());
            }

            e.Handled = true;

            //Restart
            var currentExecutablePath = Environment.ProcessPath;
            var args = Environment.GetCommandLineArgs();
            if (currentExecutablePath != null) Process.Start(currentExecutablePath, args);
            Environment.Exit(1);
        }

        public static void LogUnhandledException(object? sender, FirstChanceExceptionEventArgs e)
            => FileLog?.Fatal(e.Exception.ToString(), e.Exception);
    }
}
