using System;
using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace COMPASS.Converters
{
    public class CompositeCollectionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            CompositeCollection compositeCollection = new();

            foreach (var collection in values)
            {
                if (collection is IEnumerable enumerable)
                {
                    compositeCollection.Add(new CollectionContainer { Collection = enumerable });
                }
            }

            return compositeCollection;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

}
