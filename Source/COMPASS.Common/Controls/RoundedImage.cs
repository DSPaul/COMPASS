using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
namespace COMPASS.Common.Controls
{
    public class RoundedImage : Image
    {
        public static readonly AttachedProperty<CornerRadius> CornerRadiusProperty =
            AvaloniaProperty.RegisterAttached<RoundedImage, CornerRadius>(
                "CornerRadius", typeof(RoundedImage), new CornerRadius(5));

        public static void SetCornerRadius(AvaloniaObject element, CornerRadius parameter)
        {
            element.SetValue(CornerRadiusProperty, parameter);
        }

        public static CornerRadius GetCornerRadius(AvaloniaObject element)
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
            Clip = new RectangleGeometry(new Rect(0, 0, result.Width, result.Height), GetCornerRadius(this).TopLeft, GetCornerRadius(this).TopRight);
            return result;
        }
    }
}
