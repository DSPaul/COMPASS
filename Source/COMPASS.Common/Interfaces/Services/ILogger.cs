namespace COMPASS.Common.Interfaces.Services;

public interface ILogger
{
    void Info(string message);
    void Debug(string message, Exception? ex = null);
    void Warn(string message, Exception? ex = null);
    void Error(string message, Exception ex);
    void Fatal(string message, Exception ex);
}
