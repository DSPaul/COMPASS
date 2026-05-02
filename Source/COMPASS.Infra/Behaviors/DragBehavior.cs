using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using COMPASS.Infra.Models.DragDrop;
using static COMPASS.Infra.Tools.VisualTreeHelpers;

namespace COMPASS.Infra.Behaviors;

public sealed class DragBehavior : AvaloniaObject
{
    #region Attached Properties

    public static readonly AttachedProperty<bool> IsDragSourceProperty =
        AvaloniaProperty.RegisterAttached<DragBehavior, Control, bool>("IsDragSource");

    public static bool GetIsDragSource(Control c) => c.GetValue(IsDragSourceProperty);
    public static void SetIsDragSource(Control c, bool v) => c.SetValue(IsDragSourceProperty, v);


    public static readonly AttachedProperty<DragHandler?> DragHandlerProperty =
        AvaloniaProperty.RegisterAttached<DragBehavior, Control, DragHandler?>("DragHandler");

    public static DragHandler? GetDragHandler(Control c) => c.GetValue(DragHandlerProperty);
    public static void SetDragHandler(Control c, DragHandler? v) => c.SetValue(DragHandlerProperty, v);
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

        _lastPressedArgs = e;
        _lastPressedControl = control;
    }

    private static async void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_lastPressedArgs is null || _lastPressedControl is null) return;
        if (sender is not Visual draggedVisual || draggedVisual != _lastPressedControl) return;
            
        var pressedArgs = _lastPressedArgs;
        _lastPressedArgs = null;
        _lastPressedControl = null;
        
        if (!e.Properties.IsLeftButtonPressed) return;

        var dragDataTranfer = new DataTransfer();

        DragHandler? handler = FindInheritedValue(draggedVisual, DragHandlerProperty);
        if (handler is null) return;

        foreach (var dragConfig in handler.GetConfigs())
        {
            dragConfig.TryAddToTransfer(dragDataTranfer, draggedVisual);
        }

        var allowedEffects = DragDropEffects.Move | DragDropEffects.Copy | DragDropEffects.Link;
        await DragDrop.DoDragDropAsync(pressedArgs, dragDataTranfer, allowedEffects);
    }

    private static async void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        //if last press is not null, means no movement happened, so we consider it a click
        if (_lastPressedArgs != null && _lastPressedControl != null)
        {
            DragHandler? handler = FindInheritedValue(_lastPressedControl, DragHandlerProperty);
            handler?.ClickHandler?.Invoke(_lastPressedControl, _lastPressedArgs);
            _lastPressedArgs = null;
            _lastPressedControl = null;
        }
    }

    #endregion
}
