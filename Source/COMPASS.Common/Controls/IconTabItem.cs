using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using COMPASS.Common.Models.Enums;
using Material.Icons;

namespace COMPASS.Common.Controls;

public class IconTabItem : TabItem
{
    public static readonly StyledProperty<MaterialIconKind> IconProperty =
        AvaloniaProperty.Register<CollapsableTabItem, MaterialIconKind>(nameof(Icon));
        
    public MaterialIconKind Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly StyledProperty<bool> HideHeaderProperty = AvaloniaProperty.Register<IconTabItem, bool>(
        nameof(HideHeader));

    public bool HideHeader
    {
        get => GetValue(HideHeaderProperty);
        set => SetValue(HideHeaderProperty, value);
    }
    
    public static readonly StyledProperty<Severity> AttentionSeverityProperty =
        AvaloniaProperty.Register<CollapsableTabItem, Severity>(nameof(AttentionSeverity));
        
    public Severity AttentionSeverity
    {
        get => GetValue(AttentionSeverityProperty);
        set => SetValue(AttentionSeverityProperty, value);
    }
    
    public static readonly StyledProperty<bool> ShowAttentionProperty =
        AvaloniaProperty.Register<CollapsableTabItem, bool>(nameof(ShowAttention));
        
    public bool ShowAttention
    {
        get => GetValue(ShowAttentionProperty);
        set => SetValue(ShowAttentionProperty, value);
    }

    protected virtual void TabItemClicked(object? sender, TappedEventArgs e)
    {
        ShowAttention = false;
    }
}