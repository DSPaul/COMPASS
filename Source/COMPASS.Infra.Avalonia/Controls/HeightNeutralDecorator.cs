using Avalonia;
using Avalonia.Controls;

namespace COMPASS.Infra.Avalonia.Controls;

/// <summary>
/// Hosts a single child and stretches it to fill all the space its parent offers,
/// while reporting a preferred height of zero so that it doesn't 'push' its parent to be taller.
/// <para>
/// Use this to sit a tall, scrollable panel beside a sibling that should define the
/// height of a shared <see cref="Grid"/> row. The child fills whatever height the row
/// ends up with (so it grows and shrinks with the window), but because it contributes
/// no height of its own it can never inflate the row to fit its content.
/// </para>
/// <para>
/// Binding a sibling's height to the child directly would create a circular measure
/// dependency (the row grows to fit the child, the child's bound height then grows to
/// fit the row); this decorator breaks that cycle.
/// </para>
/// </summary>
public class HeightNeutralDecorator : Decorator
{
    protected override Size MeasureOverride(Size availableSize)
    {
        Control? child = Child;
        if (child is null)
        {
            return default;
        }

        child.Measure(availableSize);

        // Report the child's width so auto-sized columns can still fit the content,
        // but no height: the child must not influence how tall the parent becomes.
        return new Size(child.DesiredSize.Width, 0);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Control? child = Child;
        if (child is not null)
        {
            // Re-measure with the final size so scrolling uses the real viewport
            // instead of the unbounded height used during the initial measure.
            child.Measure(finalSize);
            child.Arrange(new Rect(finalSize));
        }

        return finalSize;
    }
}
