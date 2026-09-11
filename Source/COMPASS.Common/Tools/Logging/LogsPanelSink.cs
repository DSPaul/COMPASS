using COMPASS.Common.ViewModels.SidePanels;
using COMPASS.Infra.Models.Enums;
using Serilog.Core;
using Serilog.Events;

namespace COMPASS.Common.Tools.Logging;

/// <summary>
/// Serilog sink that forwards log events to the in-app Logs panel.
/// Debug events are skipped (the panel shows Info and above);
/// </summary>
public class LogsPanelSink : ILogEventSink
{
    public void Emit(LogEvent logEvent)
    {
        try
        {
            Severity? severity = logEvent.Level switch
            {
                LogEventLevel.Information => Severity.Info,
                LogEventLevel.Warning => Severity.Warning,
                LogEventLevel.Error => Severity.Error,
                LogEventLevel.Fatal => Severity.Error,
                _ => null,
            };

            if (severity is null) return;

            LogsVM.AddLog(new(severity.Value, logEvent.RenderMessage()));
        }
        catch (Exception)
        {
            //Sinks run on the caller's thread (cover fetch, web callbacks) and must never
            //throw: a logging failure must not take down the worker being logged from.
            //Nothing to log to here by definition, so the event is dropped.
        }
    }
}
