using System.Diagnostics;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services;
using COMPASS.Common.ViewModels.SidePanels;

namespace COMPASS.Common.Tools
{
    public static class Logger
    {
        public static void Init()
        {
            // Logs always go to the machine-local location so they remain accessible
            // regardless of where the user has moved their data.
            string localPath = IApplicationDataService.ApplicationDataPath;
            Directory.CreateDirectory(Path.Combine(localPath, "logs"));
            log4net.GlobalContext.Properties["DataPath"] = localPath;
            log4net.Config.XmlConfigurator.Configure(new FileInfo("log4net.config"));
            FileLog = log4net.LogManager.GetLogger(nameof(Logger));
            Info($"Launching Compass v{ApplicationService.Version}");
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
                var stackTrace = new StackTrace(1, true);
                FileLog?.Warn($"{message}\n" +
                              $"Stack trace:\n" +
                              $"{stackTrace}");
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
