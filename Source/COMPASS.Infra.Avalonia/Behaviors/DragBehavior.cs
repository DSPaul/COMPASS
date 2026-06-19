using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaInput = Avalonia.Input;
using Avalonia.VisualTree;
using COMPASS.Infra.Avalonia.DragDrop;
using COMPASS.Infra.Avalonia.ExtensionMethods;

namespace COMPASS.Infra.Avalonia.Behaviors;

public sealed class DragBehavior : AvaloniaObject
{
    #region Attached Properties

    public static readonly AttachedProperty<bool> IsDragSourceProperty =
        AvaloniaProperty.RegisterAttached<DragBehavior, Control, bool>("IsDragSource");

    public static bool GetIsDragSource(Control c) => c.GetValue(IsDragSourceProperty);
    public static void SetIsDragSource(Control c, bool v) => c.SetValue(IsDragSourceProperty, v);


    public static readonly AttachedProperty<DragManager?> DragManagerProperty =
        AvaloniaProperty.RegisterAttached<DragBehavior, Control, DragManager?>("DragManager");

    public static DragManager? GetDragManager(Control c) => c.GetValue(DragManagerProperty);
    public static void SetDragManager(Control c, DragManager? v) => c.SetValue(DragManagerProperty, v);
    #endregion

    #region Event Wiring

    static DragBehavior()
    {
        IsDragSourceProperty.Changed.AddClassHandler<Control>(OnIsDragSourceChanged);
    }

    private static void OnIsDragSourceChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            control.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, handledEventsToo: false);
            control.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, handledEventsToo: false);
            control.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, handledEventsToo: false);
        }
        else
        {
            control.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
            control.RemoveHandler(InputElement.PointerMovedEvent, OnPointerMoved);
            control.RemoveHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
        }
    }

    #endregion

    #region Drag Start

    private static PointerPressedEventArgs? _lastPressedArgs;
    private static Control? _lastPressedControl;

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        //Middle and left mouse click and such also trigger pointer pressed
        if (!e.Properties.IsLeftButtonPressed) return;

        // Skip if the press originated from an interactive child (e.g. a Button).
        Visual? ancestor = e.Source as Visual;
        while (ancestor is not null && ancestor != control)
        {
            if (ancestor is Button) return;
            ancestor = ancestor.GetVisualParent();
        }

        _lastPressedArgs = e;
        _lastPressedControl = control;
    }

    private static async void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_lastPressedArgs is null || _lastPressedControl is null) return;
        if (sender is not Visual draggedVisual || draggedVisual != _lastPressedControl) return;

        //Require at least 5 pixels of movement to start a drag operation
        var moveVector = e.GetPosition(draggedVisual) - _lastPressedArgs.GetPosition(draggedVisual);
        if (Math.Sqrt(moveVector.X * moveVector.X + moveVector.Y * moveVector.Y) < 5) return;

        var pressedArgs = _lastPressedArgs;
        _lastPressedArgs = null;
        _lastPressedControl = null;
        
        if (!e.Properties.IsLeftButtonPressed) return;

        var dragDataTranfer = new DataTransfer();

        DragManager? dragManager = draggedVisual.FindInheritedValue(DragManagerProperty);
        if (dragManager is null) return;

        foreach (var dragHandler in dragManager.GetHandlers())
        {
            dragHandler.TryAddToTransfer(dragDataTranfer, draggedVisual);
        }

        var allowedEffects = DragDropEffects.Move | DragDropEffects.Copy | DragDropEffects.Link;
        await AvaloniaInput.DragDrop.DoDragDropAsync(pressedArgs, dragDataTranfer, allowedEffects);
    }

    private static async void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        //if last press is not null, means no movement happened, so we consider it a click
        if (_lastPressedArgs != null && _lastPressedControl != null)
        {
            DragManager? dragManager = _lastPressedControl.FindInheritedValue(DragManagerProperty);
            dragManager?.ClickHandler?.Invoke(_lastPressedControl, _lastPressedArgs);
            _lastPressedArgs = null;
            _lastPressedControl = null;
        }
    }

    #endregion
}
