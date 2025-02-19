﻿using Autofac;
using Avalonia.Threading;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Models;
using COMPASS.Common.Models.ApiDtos;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using COMPASS.Common.ViewModels.SidePanels;

namespace COMPASS.Common.Tools
{
    public static class Logger
    {
        public static void Init()
        {
            log4net.GlobalContext.Properties["CompassDataPath"] = App.Container.Resolve<IEnvironmentVarsService>().CompassDataPath;
            log4net.Config.XmlConfigurator.Configure(new FileInfo("log4net.config"));
            FileLog = log4net.LogManager.GetLogger(nameof(Logger));
            //if (Application.Current is not null)
            //{
            //    Application.Current.DispatcherUnhandledException += LogUnhandledException;
            //}
            Info($"Launching Compass v{Reflection.Version}");
        }

        // Log To file
        public static log4net.ILog? FileLog { get; private set; }
        
        public static void Info(string message) => LogsVM.AddLog(new(Severity.Info, message));

        public static void Debug(string message) => FileLog?.Debug(message);


        public static void Warn(string message, Exception? ex = null)
        {
            LogsVM.AddLog(new(Severity.Warning, message));
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
            LogsVM.AddLog(new(Severity.Error, message));
            FileLog?.Error(message, ex);
        }

        public static void LogUnhandledException(object sender, Exception e)
        {
            FileLog?.Fatal(e.ToString(), e);

            //prompt user to submit logs and open an issue
            string message = $"An unexpected error ocurred.\n" +
                $"{e.Message}" +
                $"You can help improve COMPASS by opening an issue on {Constants.RepoURL} with the error message. \n" +
                $"Please include the log file located at {App.Container.Resolve<IEnvironmentVarsService>().CompassDataPath}\\logs";

            Notification crashNotification = new($"COMPASS ran into a critical error.", message, Severity.Error);
            string restartOption = "Restart COMPASS.";
            string submitOption = "Submit an anonymous crash report.";
            crashNotification.Options.Add(new(restartOption, true));
            crashNotification.Options.Add(new(submitOption, true));

            App.Container.ResolveKeyed<INotificationService>(NotificationDisplayType.Windowed).Show(crashNotification);

            if (crashNotification.IsOptionSelected(submitOption))
            {
                var report = new CrashReport(e);
                var result = new ApiClientService().PostAsync(report, "submit/crash").Result;
                Info(result.ToString());
            }

            //Restart
            if (crashNotification.IsOptionSelected(restartOption))
            {
                var currentExecutablePath = Environment.ProcessPath;
                var args = Environment.GetCommandLineArgs();
                if (currentExecutablePath != null) Process.Start(currentExecutablePath, args);
            }

            Environment.Exit(1);
        }

        public static void LogUnhandledException(object? sender, FirstChanceExceptionEventArgs e)
            => FileLog?.Fatal(e.Exception.ToString(), e.Exception);
    }
}
