namespace COMPASS.Models
{
    public class LogEntry : ObservableObject
    {
        public LogEntry(MsgType msgType, string message)
        {
            Type = msgType;
            Msg = message;
        }

        private MsgType _type;
        public MsgType Type
        {
            get { return _type; }
            set { SetProperty(ref _type, value); }
        }

        private string _msg;
        public string Msg
        {
            get { return _msg; }
            set { SetProperty(ref _msg, value); }
        }

        public enum MsgType
        {
            Info,
            Warning,
            Error
        }
    }
}
