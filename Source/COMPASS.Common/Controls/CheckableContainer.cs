using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace COMPASS.Common.Controls;

public class CheckableContainer : HeaderedContentControl
{
    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<CheckableContainer, bool>(nameof(IsChecked),
            defaultBindingMode: BindingMode.TwoWay);

    public bool IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    static CheckableContainer()
    {
        IsCheckedProperty.Changed.AddClassHandler<CheckableContainer>((x, _) => x.UpdatePseudoClasses());
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        UpdatePseudoClasses();
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":checked", IsChecked);
    }
}
