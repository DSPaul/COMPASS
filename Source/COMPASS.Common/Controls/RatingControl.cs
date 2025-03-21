using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using System.Collections.Generic;
using System.Linq;

namespace COMPASS.Common.Controls;

//Based on https://github.com/AvaloniaUI/Avalonia.Samples/tree/main/src/Avalonia.Samples/CustomControls/RatingControlSample

// This Attribute specifies that "PART_StarsPresenter" is a control, which must be present in the Control-Template
[TemplatePart("PART_StarsPresenter", typeof(ItemsControl))]
public class RatingControl : TemplatedControl 
{
    private ItemsControl? _starsPresenter;
 
    public RatingControl() 
    { 
        UpdateStars();
    }
    
    public static readonly StyledProperty<int> NumberOfStarsProperty =
        AvaloniaProperty.Register<RatingControl, int>(
            nameof(NumberOfStars),
            defaultValue: 5,
            coerce: CoerceNumberOfStars);

    
    public int NumberOfStars
    {
        get => GetValue(NumberOfStarsProperty);
        set => SetValue(NumberOfStarsProperty, value);
    }

    /// <summary>
    /// This function will coerce the <see cref="NumberOfStars"/> property. The minimum allowed number is 0
    /// </summary>
    /// <param name="sender">the RatingControl-instance calling this method</param>
    /// <param name="value">the value to coerce</param>
    /// <returns>The coerced value</returns>
    private static int CoerceNumberOfStars(AvaloniaObject sender, int value) => Math.Max(1, value);
    
    public static readonly DirectProperty<RatingControl, int> ValueProperty =
        AvaloniaProperty.RegisterDirect<RatingControl, int>(
            nameof(Value),  
            o => o.Value,
            (o, v) => o.Value = v,
            defaultBindingMode: BindingMode.TwoWay,
            enableDataValidation: true);
    
    private int _value;
    public int Value
    {
        get => _value;
        set => SetAndRaise(ValueProperty, ref _value, value);
    }


    /// <summary>
    /// Defines the <see cref="Stars"/> property.
    /// </summary>
    /// <remarks>
    /// ´This property holds a read-only array of stars. 
    /// </remarks>
    public static readonly DirectProperty<RatingControl, IEnumerable<int>> StarsProperty =
        AvaloniaProperty.RegisterDirect < RatingControl, IEnumerable<int>>(nameof(Stars), o => o.Stars);

    // For read-only properties we need to have a backing field. The default value is [1..5]
    private IEnumerable<int> _stars = Enumerable.Range(1, 5);

    /// <summary>
    /// Gets the current collection of visible stars
    /// </summary>
    public IEnumerable<int> Stars
    {
        get => _stars;
        private set => SetAndRaise(StarsProperty, ref _stars, value); // make sure the setter is private
    }

    // called when the number of stars changed
    private void UpdateStars()
    {
        Stars = Enumerable.Range(1, NumberOfStars);
    }
    
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // if the changed property is the NumberOfStarsProperty, we need to update the stars
        if (change.Property == NumberOfStarsProperty) 
        {
            UpdateStars();
        }
    }

    // We override what happens when the control template is being applied. 
    // That way we can for example listen to events of controls which are part of the template
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // if we had a control template before, we need to unsubscribe any event listeners
        if(_starsPresenter is not null)
        {
            _starsPresenter.PointerReleased-= StarsPresenter_PointerReleased;
        }

        // try to find the control with the given name
        _starsPresenter = e.NameScope.Find("PART_StarsPresenter") as ItemsControl;

        // listen to pointer-released events on the stars presenter.
        if(_starsPresenter != null)
        {
            _starsPresenter.PointerReleased += StarsPresenter_PointerReleased;
        }
    }

    /// <summary>
    /// Called to update the validation state for properties for which data validation is enabled
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="state">The current data binding state.</param>
    /// <param name="error">The Exception that was passed</param>
    protected override void UpdateDataValidation(AvaloniaProperty property, BindingValueType state, Exception? error)
    {
        base.UpdateDataValidation(property, state, error);
        
        if(property == ValueProperty)
        {
            DataValidationErrors.SetError(this, error);
        }
    }

    private void StarsPresenter_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        if (e.Source is Path star)
        {
            Value = star.DataContext as int? ?? 0;
        }
    }
}