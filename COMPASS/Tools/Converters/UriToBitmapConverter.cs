using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace COMPASS.Tools.Converters
{
    class UriToBitmapConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (File.Exists((string)value))
            {
                //Parameter is true if instant refresh is needed for Edit View, false for mass view
                bool Editview = (parameter == null)? false: (bool)parameter;
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();

                if (Editview)
                {
                    bi.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                }
                else
                {
                    bi.CreateOptions = BitmapCreateOptions.DelayCreation;
                    bi.DecodePixelWidth = 200;
                }

                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.UriSource = new Uri(value.ToString());
                bi.EndInit();
                return bi;
            }
            return null;
        }
  
       public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }
}
