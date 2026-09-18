using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Enums;
using COMPASS.Infra.Tools.Logging;

namespace COMPASS.Infra.ExtensionMethods
{
    public static class InfraExtensions
    {
        public static void Log(this ILogger logger, LogEntry logEntry)
        {
            switch (logEntry.Severity)
            {
                case Severity.Info:
                    logger.Info(logEntry.Msg);
                    break;
                case Severity.Warning:
                    logger.Warn(logEntry.Msg);
                    break;
                case Severity.Error:
                    logger.Error(logEntry.Msg, new Exception(logEntry.Msg));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
