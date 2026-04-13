using System.Diagnostics;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;

namespace COMPASS.Common.Tools.Logging;

public class FileLogger : ILogger
{
    private readonly log4net.ILog _log;

    public FileLogger()
    {
        string localPath = IApplicationDataService.ApplicationDataPath;
        Directory.CreateDirectory(Path.Combine(localPath, "logs"));
        log4net.GlobalContext.Properties["DataPath"] = localPath;
        log4net.Config.XmlConfigurator.Configure(new FileInfo("log4net.config"));
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
