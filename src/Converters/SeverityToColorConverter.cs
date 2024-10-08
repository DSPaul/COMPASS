﻿using COMPASS.Models.Enums;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace COMPASS.Converters
{
    class SeverityToColorConverter : IValueConverter
    {

        public Color InfoColor { get; set; } = Colors.LightGray;
        public Color WarningColor { get; set; } = (Color)ColorConverter.ConvertFromString("#FFB800");
        public Color ErrorColor { get; set; } = Colors.Red;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
