using Avalonia.Data.Converters;
using Avalonia.Media;
using COMPASS.Common.Models;
using System;
using System.Globalization;

namespace COMPASS.Common.Converters
{
    class MsgTypeToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is LogEntry.MsgType msgType)
            {
                return msgType switch
                {
                    LogEntry.MsgType.Info => new SolidColorBrush(Colors.LightGray),
                    LogEntry.MsgType.Warning => new SolidColorBrush(Colors.Yellow),
                    LogEntry.MsgType.Error => new SolidColorBrush(Colors.Red),
                    _ => new SolidColorBrush(Colors.Pink),//should never happen, Bright pink so it's clear something went wrong
                };
            }

            return new SolidColorBrush(Colors.Purple); //should never happen, purple so it's clear something went wrong
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
