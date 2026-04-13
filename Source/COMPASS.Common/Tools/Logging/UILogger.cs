using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.ViewModels.SidePanels;

namespace COMPASS.Common.Tools.Logging;

public class UILogger : ILogger
{
    public void Info(string message) => LogsVM.AddLog(new(Severity.Info, message));

    public void Debug(string message, Exception? ex = null) { }

    public void Warn(string message, Exception? ex = null) => LogsVM.AddLog(new(Severity.Warning, message));

    public void Error(string message, Exception ex) => LogsVM.AddLog(new(Severity.Error, message));

    public void Fatal(string message, Exception ex) { }
}
