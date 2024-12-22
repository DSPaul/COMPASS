using Avalonia;
using Avalonia.Controls;

namespace COMPASS.Common.Controls;

public class FilterModeSelector : UserControl
{
    public static readonly DirectProperty<FilterModeSelector, bool> IncludeProperty = 
        AvaloniaProperty.RegisterDirect<FilterModeSelector, bool>(
            nameof(Include),
            o => o.Include,
            (o,v) => o.Include = v,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);
    
    private bool _include = true;

    //Bool for now, as only include and exclude exist, might get upgraded to enum later on
    public bool Include
    {
        get => _include;
        set => SetAndRaise(IncludeProperty, ref _include, value);
    }
}