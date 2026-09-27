namespace COMPASS.Infra.Logging
{
    public static class LoggingExtensions
    {
        extension(ILogger logger)
        {
            public void Log(LogEntry logEntry)
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
}
