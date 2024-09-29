using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace COMPASS.Common.Converters
{
    public class CompositeCollectionConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            //TODO
            object? compositeCollection = null;

            //CompositeCollection compositeCollection = new();

            //foreach (var collection in values)
            //{
            //    if (collection is IEnumerable enumerable)
            //    {
            //        compositeCollection.Add(new CollectionContainer { Collection = enumerable });
            //    }
            //}

            return compositeCollection;
        }
    }

}
