namespace COMPASS.Infra.Tools.Logging;

/// <summary>
/// Decorator that forwards logs to both a permanent logger
/// And the current scoped logger if any
/// </summary>
public sealed class ScopedForwardingLogger(ILogger innerLogger) : ILogger
{
    public void Info(string message)
    {
        //If there is a local scope, keep info logs contained to that scope
        if(LoggerScope.Current == null)
        {
            innerLogger.Info(message);
        }
        LoggerScope.Current?.Info(message);
    }

    public void Debug(string message, Exception? ex = null)
    {
        innerLogger.Debug(message, ex);
        LoggerScope.Current?.Debug(message, ex);
    }

    public void Warn(string message, Exception? ex = null)
    {
        innerLogger.Warn(message, ex);
        LoggerScope.Current?.Warn(message, ex);
    }

    public void Error(string message, Exception ex)
    {
        innerLogger.Error(message, ex);
        LoggerScope.Current?.Error(message, ex);
    }

    public void Fatal(string message, Exception ex)
    {
        innerLogger.Fatal(message, ex);
        LoggerScope.Current?.Fatal(message, ex);
    }
}
