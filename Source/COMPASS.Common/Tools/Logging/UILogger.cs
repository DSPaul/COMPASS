using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.ViewModels.SidePanels;
using COMPASS.Infra.Models.Enums;

namespace COMPASS.Common.Tools.Logging;

public class UILogger : ILogger
{
    public void Info(string message) => LogsVM.AddLog(new(Severity.Info, message));

    public void Debug(string message, Exception? ex = null) { }

    public void Warn(string message, Exception? ex = null) => LogsVM.AddLog(new(Severity.Warning, message));

    public void Error(string message, Exception ex) => LogsVM.AddLog(new(Severity.Error, message));

    public void Fatal(string message, Exception ex) { }
}
