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
            return msgtype switch
            {
                LogEntry.MsgType.Info => new SolidColorBrush(Colors.LightGray),
                LogEntry.MsgType.Warning => new SolidColorBrush(Colors.Yellow),
                LogEntry.MsgType.Error => new SolidColorBrush(Colors.Red),
                _ => new SolidColorBrush(Colors.Pink),//should never happen, Bright pink to it's clear something went wrong
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
