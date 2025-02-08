using System;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace COMPASS.Converters
{
    class UriToBitmapConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object getFullRes, System.Globalization.CultureInfo culture)
        {
            if (value is not string path) return null;
            if (!File.Exists(path)) return null;
            bool fullRes = System.Convert.ToBoolean(getFullRes);

            BitmapImage bi = new();
            bi.BeginInit();

            bi.CreateOptions = fullRes ? BitmapCreateOptions.IgnoreImageCache : BitmapCreateOptions.DelayCreation;
            bi.CacheOption = BitmapCacheOption.OnLoad;

            try
            {
                bi.UriSource = new Uri(path);
                bi.EndInit();
                if (bi.CanFreeze)
                {
                    bi.Freeze();
                }
                return bi;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException("The method or operation is not implemented.");
    }
}
