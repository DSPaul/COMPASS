using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
namespace COMPASS.Common.Controls
{
    public class RoundedImage : Image
    {
        public static readonly AttachedProperty<double> CornerRadiusProperty =
            AvaloniaProperty.RegisterAttached<RoundedImage, double>(
                "CornerRadius", typeof(RoundedImage), 5);

        public static void SetCornerRadius(AvaloniaObject element, double parameter)
        {
            element.SetValue(CornerRadiusProperty, parameter);
        }

        public static double GetCornerRadius(AvaloniaObject element)
        {
            return element.GetValue(CornerRadiusProperty);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            IImage? source = Source;
            Size result = new();

            if (source != null)
            {
                result = Stretch.CalculateSize(availableSize, source.Size, StretchDirection);
            }
            Clip = new RectangleGeometry(new Rect(0, 0, result.Width, result.Height), GetCornerRadius(this), GetCornerRadius(this));
            return result;
        }
    }
}
