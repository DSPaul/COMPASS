using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Common.Models.Enums;

namespace COMPASS.Common.Models
{
    public class LogEntry : ObservableObject
    {
        public LogEntry(Severity severity, string message)
        {
            _severity = severity;
            _msg = message;
        }

        private Severity _severity;
        public Severity Severity
        {
            get => _severity;
            set => SetProperty(ref _severity, value);
        }

        private string _msg;
        public string Msg
        {
            get => _msg;
            set => SetProperty(ref _msg, value);
        }
    }
}
