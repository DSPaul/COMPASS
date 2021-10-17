using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COMPASS.Models
{
    public class LogEntry : ObservableObject
    {
        public LogEntry(MsgType msgType, string message)
        {
            Type = msgType;
            Msg = message;
        }

        private MsgType _Type;
        public MsgType Type
        {
            get { return _Type; }
            set { SetProperty(ref _Type, value); }
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
