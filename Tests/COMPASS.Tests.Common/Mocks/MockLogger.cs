using COMPASS.Infra.Tools.Logging;

namespace COMPASS.Tests.Common.Mocks;

public class MockLogger : ILogger
{
    public List<string> Debugs = [];
    public List<string> Infos = [];
    public List<string> Warnings = [];
    public List<string> Errors = [];
    public List<string> Fatals = [];

    public void Info(string message)
    {
        Infos.Add(message);
        Console.WriteLine($"[INFO]  {message}");
    }

    public void Debug(string message, Exception? ex = null)
    {
        Debugs.Add(message);
        Console.WriteLine($"[DEBUG] {message}{FormatException(ex)}");
    }

    public void Warn(string message, Exception? ex = null)
    {
        Warnings.Add(message);
        Console.WriteLine($"[WARN]  {message}{FormatException(ex)}");
    }

    public void Error(string message, Exception ex)
    {
        Errors.Add(message);
        Console.WriteLine($"[ERROR] {message}{FormatException(ex)}");
    }

    public void Fatal(string message, Exception ex)
    {
        Fatals.Add(message);
        Console.WriteLine($"[FATAL] {message}{FormatException(ex)}");
    }

    private static string FormatException(Exception? ex) => ex is null ? "" : $" | {ex.GetType().Name}: {ex.Message}";
}
