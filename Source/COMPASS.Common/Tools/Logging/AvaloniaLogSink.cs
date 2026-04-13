using Avalonia;
using Avalonia.Logging;
using COMPASS.Infra.Tools;
using System.Diagnostics;

namespace COMPASS.Common.Tools.Logging;

public class AvaloniaLogSink(FileLogger fileLogger) : ILogSink
{
    public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning;

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
        => Log(level, area, source, messageTemplate, []);

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
    {
        string message = FormatMessage(area, source, messageTemplate, propertyValues);

        Trace.WriteLine(message);

        switch (level)
        {
            case LogEventLevel.Warning:
                fileLogger.Warn(message);
                break;
            case LogEventLevel.Error:
                fileLogger.Error(message, new Exception(message));
                break;
            case LogEventLevel.Fatal:
                fileLogger.Fatal(message, new Exception(message));
                break;
        }
    }

    private static string FormatMessage(string area, object? source, string messageTemplate, object?[] propertyValues)
    {
        string formatted;
        try { formatted = string.Format(messageTemplate, propertyValues); }
        catch { formatted = messageTemplate; }

        string sourceName = source?.GetType().Name ?? "Unknown";
        return $"[Avalonia/{area}] ({sourceName}) {formatted}";
    }
}

public static class AvaloniaLogSinkExtensions
{
    public static AppBuilder LogToFileLogger(this AppBuilder builder)
    {
        Logger.Sink = new AvaloniaLogSink(ServiceResolver.Resolve<FileLogger>());
        return builder;
    }
}
