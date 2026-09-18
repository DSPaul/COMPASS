using System.Diagnostics;

namespace COMPASS.Infra.Models.Progress
{
    public interface IProgressReport
    {
    }

    public static class ProgressReports
    {
        public static IProgressReport XOutOfY(long value, long total) => new XOutOfYReport(value, total);
        public static IProgressReport Percentage(double percentage) => new FractionalReport(percentage);
        public static IProgressReport Status(string message) => new StatusReport(message);
        public static IProgressReport Log(LogEntry logMessage) => new LogReport(logMessage);
        
        private static readonly IncrementReport _incrementReport = new();
        public static IProgressReport Increment => _incrementReport;

        private static readonly CompletedReport _completedReport = new();
        public static IProgressReport Completed => _completedReport;
    }

    internal readonly struct XOutOfYReport : IProgressReport
    {
        public long Value { get; }
        public long Total { get; }
        public XOutOfYReport(long value, long total)
        {
            Value = value;
            Total = total;
        }
    }

    internal readonly struct FractionalReport : IProgressReport
    {
        public double Fraction { get; }
        public FractionalReport(double fraction)
        {
            Debug.Assert(fraction >= 0);
            Debug.Assert(fraction <= 1);
            Fraction = fraction;
        }
    }

    internal readonly struct StatusReport : IProgressReport
    {
        public string Status { get; }
        public StatusReport(string status)
        {
            Status = status;
        }
    }

    internal readonly struct LogReport : IProgressReport
    {
        public LogEntry LogMessage { get; }
        public LogReport(LogEntry logMessage)
        {
            LogMessage = logMessage;
        }
    }

    internal readonly struct IncrementReport : IProgressReport { }

    internal readonly struct CompletedReport : IProgressReport { }
}
