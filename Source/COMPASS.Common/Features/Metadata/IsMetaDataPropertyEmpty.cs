using Avalonia.Data;
using Avalonia.Data.Converters;
using System.Globalization;

namespace COMPASS.Common.Features.Metadata
{
    public class IsMetadataPropertyEmpty : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                var exception = new ArgumentNullException(nameof(value));
                return new BindingNotification(exception, BindingErrorType.Error);
            }

            if (parameter == null)
            {
                var exception = new ArgumentNullException(nameof(parameter));
                return new BindingNotification(exception, BindingErrorType.Error);
            }
            
            CodexProperty? prop = CodexProperty.GetInstance(parameter.ToString() ?? "");

            if (prop is null)
            {
                var exception = new ArgumentException($"Property {parameter} could not be found");
                return new BindingNotification(exception, BindingErrorType.Error);
            }

            return value switch
            {
                SourceMetadataViewModel metaDataVM => !prop.IsEmpty(metaDataVM.GetSourceMetadata()),
                IHasCodexMetadata metadata => !prop.IsEmpty(metadata),
                _ => false
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
