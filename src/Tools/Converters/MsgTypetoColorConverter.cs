using COMPASS.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace COMPASS.Tools.Converters
{
    class MsgTypetoColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var msgtype = (LogEntry.MsgType)value;
            switch (msgtype)
            {
                case LogEntry.MsgType.Info:
                    return new SolidColorBrush(Colors.LightGray);
                case LogEntry.MsgType.Warning:
                    return new SolidColorBrush(Colors.Yellow);
                case LogEntry.MsgType.Error:
                    return new SolidColorBrush(Colors.Red);
                default:
                    //should never happen, Bright pink to it's clear something went wrong
                    return new SolidColorBrush(Colors.Pink);
            }   
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
