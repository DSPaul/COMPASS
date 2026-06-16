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

        public CornerRadius CornerRadius
        {
            get => GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            IImage? source = Source;
            Size bounds = new();

            if (source != null)
            {
                bounds = Stretch.CalculateSize(availableSize, source.Size, StretchDirection);
            }

            if(CornerRadius == default)
            {
                Clip = new RectangleGeometry(new Rect(bounds));
            }
            else if (CornerRadius.IsUniform)
            {
                Clip = new RectangleGeometry(new Rect(bounds), radiusX: CornerRadius.TopLeft, radiusY: CornerRadius.TopRight);
            }
            else
            {
                var mask = new PathGeometry();
                Point start = new Point(CornerRadius.TopLeft, 0);
                mask.Figures!.Add(new PathFigure
                {
                    StartPoint = start,
                    Segments = new PathSegments
                    {
                        new LineSegment { Point = new Point(bounds.Width - CornerRadius.TopRight, 0) },
                        new ArcSegment
                        {
                            Point = new Point(bounds.Width, CornerRadius.TopRight),
                            Size = new Size(CornerRadius.TopRight, CornerRadius.TopRight),
                            SweepDirection = SweepDirection.Clockwise
                        },
                        new LineSegment { Point = new Point(bounds.Width, bounds.Height - CornerRadius.BottomRight)},
                        new ArcSegment
                        {
                            Point = new Point(bounds.Width - CornerRadius.BottomRight, bounds.Height),
                            Size = new Size(CornerRadius.BottomRight, CornerRadius.BottomRight), 
                            SweepDirection = SweepDirection.Clockwise
                        },
                        new LineSegment { Point = new Point(CornerRadius.BottomLeft, bounds.Height) },
                        new ArcSegment
                        {
                            Point = new Point(0, bounds.Height - CornerRadius.BottomLeft),
                            Size = new Size(CornerRadius.BottomLeft, CornerRadius.BottomLeft),
                            SweepDirection = SweepDirection.Clockwise
                        },
                        new LineSegment { Point = new Point(0, CornerRadius.TopLeft)},
                        new ArcSegment
                        {
                            Point = start,
                            Size = new Size(CornerRadius.TopLeft, CornerRadius.TopLeft),
                            SweepDirection = SweepDirection.Clockwise,
                        },
                    },
                    IsClosed = true
                });

                Clip = mask;
            }

            return bounds;
        }
    }
}
