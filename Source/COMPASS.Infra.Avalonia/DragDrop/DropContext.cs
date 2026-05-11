using Avalonia;
using Avalonia.Input;

namespace COMPASS.Infra.Avalonia.DragDrop;

/// <summary>
/// Positional and visual context available during a drop interaction.
/// Allows drop handlers to make position-dependent decisions (e.g. insertion index).
/// </summary>
public sealed class DropContext
{
    /// <summary>
    /// The visual that received the drop event (the control with IsDropTarget set).
    /// </summary>
    public required Visual DropTarget { get; init; }

    /// <summary>
    /// The full <see cref="DragEventArgs"/> from the Avalonia drag event.
    /// </summary>
    public required DragEventArgs DragEventArgs { get; init; }
}
