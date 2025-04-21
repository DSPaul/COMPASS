using Autofac;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Models.Enums;
using System;
using System.IO;
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
        
        public static void Fatal(string message, Exception ex)
        {
            FileLog?.Fatal(message, ex);
        }
    }
}
