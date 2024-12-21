using Avalonia.Data.Converters;
using Avalonia.Media;

using COMPASS.Common.Models.Enums;
using System;
using System.Globalization;

namespace COMPASS.Common.Converters
{
    public class SeverityToColorConverter : IValueConverter
    {
        public Color InfoColor { get; } = Colors.LightGray;
        public Color WarningColor { get; } = Color.Parse("#FFB800");
        public Color ErrorColor { get; } = Colors.Red;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Severity severity)
            {
                return severity switch
                {
                    Severity.Info => new SolidColorBrush(InfoColor),
                    Severity.Warning => new SolidColorBrush(WarningColor),
                    Severity.Error => new SolidColorBrush(ErrorColor),
                    _ => new SolidColorBrush(Colors.Pink),//should never happen, Bright pink so it's clear something went wrong
                };
            }

            return new SolidColorBrush(Colors.Purple); //should never happen, purple so it's clear something went wrong
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
