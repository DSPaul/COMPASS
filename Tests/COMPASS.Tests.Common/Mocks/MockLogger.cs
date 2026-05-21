using COMPASS.Common.Interfaces.Services;

namespace COMPASS.Tests.Common.Mocks;

public class MockLogger : ILogger
{
    public void Info(string message) => Console.WriteLine($"[INFO]  {message}");
    public void Debug(string message, Exception? ex = null) => Console.WriteLine($"[DEBUG] {message}{FormatException(ex)}");
    public void Warn(string message, Exception? ex = null) => Console.WriteLine($"[WARN]  {message}{FormatException(ex)}");
    public void Error(string message, Exception ex) => Console.WriteLine($"[ERROR] {message}{FormatException(ex)}");
    public void Fatal(string message, Exception ex) => Console.WriteLine($"[FATAL] {message}{FormatException(ex)}");

    private static string FormatException(Exception? ex) => ex is null ? "" : $" | {ex.GetType().Name}: {ex.Message}";
}
