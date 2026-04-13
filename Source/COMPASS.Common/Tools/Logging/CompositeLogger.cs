using COMPASS.Common.Interfaces.Services;

namespace COMPASS.Common.Tools.Logging;

public class CompositeLogger(IEnumerable<ILogger> loggers) : ILogger
{
    public void Info(string message)
    {
        foreach (var logger in loggers) logger.Info(message);
    }

    public void Debug(string message, Exception? ex = null)
    {
        foreach (var logger in loggers) logger.Debug(message, ex);
    }

    public void Warn(string message, Exception? ex = null)
    {
        foreach (var logger in loggers) logger.Warn(message, ex);
    }

    public void Error(string message, Exception ex)
    {
        foreach (var logger in loggers) logger.Error(message, ex);
    }

    public void Fatal(string message, Exception ex)
    {
        foreach (var logger in loggers) logger.Fatal(message, ex);
    }
}
