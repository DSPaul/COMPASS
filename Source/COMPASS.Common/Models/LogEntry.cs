using CommunityToolkit.Mvvm.ComponentModel;

namespace COMPASS.Models
{
    public class LogEntry : ObservableObject
    {
        public LogEntry(MsgType msgType, string message)
        {
            _type = msgType;
            _msg = message;
        }

        private MsgType _type;
        public MsgType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        private string _msg;
        public string Msg
        {
            get => _msg;
            set => SetProperty(ref _msg, value);
        }

        public enum MsgType
        {
            Info,
            Warning,
            Error
        }
    }
}
