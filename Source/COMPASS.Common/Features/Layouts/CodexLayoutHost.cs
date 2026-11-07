using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using COMPASS.Infra.Avalonia.Threading;

namespace COMPASS.Common.Features.Layouts;

/// <summary>
/// Shows the view of the active layout. Each layout view-model keeps its own view for as long as it lives,
/// so switching between tabs only toggles which view is visible instead of rebuilding or recycling one.
/// This keeps per-tab view state such as the selection and scroll position, and makes tab switches cheap.
/// </summary>
public class CodexLayoutHost : Panel
{
    private static readonly Dictionary<Type, Func<CodexLayoutView>> _viewFactories = [];

    /// <summary>
    /// Registers the view for a layout view-model.
    /// </summary>
    public static void Register<TViewModel, TView>()
        where TViewModel : LayoutViewModel
        where TView : CodexLayoutView, new()
        => _viewFactories[typeof(TViewModel)] = () => new TView();

    public static readonly StyledProperty<LayoutViewModel?> ActiveLayoutProperty =
        AvaloniaProperty.Register<CodexLayoutHost, LayoutViewModel?>(nameof(ActiveLayout));

    public LayoutViewModel? ActiveLayout
    {
        get => GetValue(ActiveLayoutProperty);
        set => SetValue(ActiveLayoutProperty, value);
    }

    private readonly Dictionary<LayoutViewModel, CodexLayoutView> _viewsByLayout = [];

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ActiveLayoutProperty)
        {
            ShowLayout(change.GetNewValue<LayoutViewModel?>());
        }
    }

    private void ShowLayout(LayoutViewModel? activeLayout)
    {
        if (activeLayout is not null && !_viewsByLayout.ContainsKey(activeLayout))
        {
            CodexLayoutView? layoutView = CreateView(activeLayout);
            if (layoutView is not null)
            {
                _viewsByLayout[activeLayout] = layoutView;
                activeLayout.Disposed += OnLayoutDisposed;
                Children.Add(layoutView);
            }
        }

        foreach (KeyValuePair<LayoutViewModel, CodexLayoutView> layoutAndView in _viewsByLayout)
        {
            layoutAndView.Value.IsVisible = layoutAndView.Key == activeLayout;
        }
    }

    private static CodexLayoutView? CreateView(LayoutViewModel layout)
    {
        //Go from specific to general, first VM type, than base class, etc
        Type? layoutType = layout.GetType();
        while (layoutType is not null)
        {
            if (_viewFactories.TryGetValue(layoutType, out Func<CodexLayoutView>? viewFactory))
            {
                CodexLayoutView layoutView = viewFactory();
                layoutView.DataContext = layout;
                return layoutView;
            }
            layoutType = layoutType.BaseType;
        }
        return null;
    }

    private void OnLayoutDisposed(object? sender, EventArgs e)
    {
        if (sender is not LayoutViewModel disposedLayout) return;

        Dispatcher.UIThread.PostIfNeeded(() =>
        {
            disposedLayout.Disposed -= OnLayoutDisposed;
            if (_viewsByLayout.Remove(disposedLayout, out CodexLayoutView? disposedView))
            {
                Children.Remove(disposedView);
            }
        });
    }
}
