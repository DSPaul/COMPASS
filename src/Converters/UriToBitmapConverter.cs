using System;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace COMPASS.Converters
{
    class UriToBitmapConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!File.Exists((string)value)) return null;
            //Parameter is true if instant refresh is needed for Edit View, false for mass view
            bool fullRes = System.Convert.ToBoolean(parameter);
            BitmapImage bi = new();
            bi.BeginInit();

            bi.CreateOptions = fullRes ? BitmapCreateOptions.IgnoreImageCache : BitmapCreateOptions.DelayCreation;
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.UriSource = new Uri(value.ToString());
            bi.EndInit();
            return bi;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException("The method or operation is not implemented.");
    }
}
