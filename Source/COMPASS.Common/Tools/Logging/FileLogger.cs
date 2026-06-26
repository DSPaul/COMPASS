using System.Diagnostics;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;

namespace COMPASS.Common.Tools.Logging;

public class FileLogger : ILogger
{
    private readonly log4net.ILog _log;

    public FileLogger()
    {
        string logsDirectory = Path.Combine(IApplicationDataService.ApplicationDataPath, Constants.DIR_LOGS);
        string configPath = Path.Combine(AppContext.BaseDirectory, "log4net.config");

        Directory.CreateDirectory(logsDirectory);
        log4net.GlobalContext.Properties["logsDir"] = logsDirectory;
        log4net.Config.XmlConfigurator.Configure(new FileInfo(configPath));
        _log = log4net.LogManager.GetLogger(nameof(FileLogger));
    }

    public void Info(string message) => _log.Info(message);

    public void Debug(string message, Exception? ex = null)
    {
        if (ex is null)
        {
            var stackTrace = new StackTrace(1, true);
            _log.Debug($"{message}\n" +
                       $"Stack trace:\n" +
                       $"{stackTrace}");
        }
        else
        {
            _log.Debug(message, ex);
        }
    }

    public void Warn(string message, Exception? ex = null)
    {
        if (ex is null)
        {
            var stackTrace = new StackTrace(1, true);
            _log.Warn($"{message}\n" +
                      $"Stack trace:\n" +
                      $"{stackTrace}");
        }
        else
        {
            _log.Warn(message, ex);
        }
    }

    public void Error(string message, Exception ex) => _log.Error(message, ex);

    public void Fatal(string message, Exception ex) => _log.Fatal(message, ex);
}
