using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using Serilog;
using Serilog.Events;
using ILogger = COMPASS.Infra.Tools.Logging.ILogger;

namespace COMPASS.Common.Tools.Logging;

/// <summary>
/// Single application logger: one Serilog pipeline with a rolling file sink
/// and the in-app Logs panel sink
/// </summary>
public class SerilogLogger : ILogger, IDisposable
{
    private readonly Serilog.Core.Logger _log;
    private bool _disposed;

    public SerilogLogger()
    {
        string logsDirectory = Path.Combine(IApplicationDataService.ApplicationDataPath, Constants.DIR_LOGS);
        Directory.CreateDirectory(logsDirectory);

        _log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(logsDirectory, "compass-.log"),
                rollingInterval: RollingInterval.Month,
                retainedFileCountLimit: 12,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Sink(new LogsPanelSink())
            .CreateLogger();
    }

    public void Info(string message) => _log.Write(LogEventLevel.Information, message);

    public void Debug(string message, Exception? ex = null) => _log.Write(LogEventLevel.Debug, ex, message);

    public void Warn(string message, Exception? ex = null) => _log.Write(LogEventLevel.Warning, ex, message);

    public void Error(string message, Exception ex) => _log.Write(LogEventLevel.Error, ex, message);

    public void Fatal(string message, Exception ex) => _log.Write(LogEventLevel.Fatal, ex, message);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _log.Dispose();
        GC.SuppressFinalize(this);
    }
}
