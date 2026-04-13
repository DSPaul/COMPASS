using COMPASS.Infra.Models.Enums;

namespace COMPASS.Infra.Models
{
    public struct LogEntry
    {
        public LogEntry(Severity severity, string message)
        {
            Severity = severity;
            Msg = message;
            Time = DateTime.Now;
        }

        public Severity Severity { get; }
        
        public string Msg { get; }

        public DateTime Time { get; }
    }
}
