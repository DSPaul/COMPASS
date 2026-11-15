using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using COMPASS.Infra.DependencyInjection;
using COMPASS.Infra.Logging;

namespace COMPASS.Infra.Avalonia.Modal;

public partial class ModalWindow : Window
{
    private static readonly Dictionary<Type, Func<Control>> _templates = [];

    /// <summary>
    /// Registers the view for a modal viewmodel.
    /// </summary>
    public static void Register<TViewModel, TView>()
        where TViewModel : IModalViewModel
        where TView : Control, new()
        => _templates[typeof(TViewModel)] = () => new TView();

    /// <summary>
    /// //DO NOT USE, FOR DESIGNER ONLY
    /// </summary>
    public ModalWindow()
    {
        throw new Exception("DO NOT USE PARAMETERLESS CONSTRUCTOR");
    }

    public ModalWindow(IModalViewModel vm, bool disposeOnClose = true)
    {
        InitializeComponent();
        DataTemplates.Add(new RegisteredTemplateSelector());
        DataContext = vm;
        _disposeOnClose = disposeOnClose;
        vm.CloseAction = () => Dispatcher.UIThread.Post(Close);

        AddHandler(WindowOpenedEvent, (_, _) => LimitContentSizeToScreen());

        // Wait until the layout is ready
        ContentPresenter.Loaded += (_, _) => UpdateBoundsFromContent();
    }

    private readonly bool _disposeOnClose;

    private void UpdateBoundsFromContent()
    {
        Visual? presenter = ContentPresenter.GetVisualChildren().FirstOrDefault(); //inner content presenter
        Control? modalView = presenter?.GetVisualChildren().OfType<Control>().FirstOrDefault();

        if (modalView == null) return;

        MinWidth = modalView.MinWidth;
        MinHeight = modalView.MinHeight;

        MaxWidth = modalView.MaxWidth;
        MaxHeight = modalView.MaxHeight;

        Width = modalView.Width;
        Height = modalView.Height;

        SizeToContent = SizeToContent.Manual; //don't resize modal after it's loaded

        // The screen limit is only for the initial size, after that the content should follow the window when resized
        ContentPresenter.MaxWidth = double.PositiveInfinity;
        ContentPresenter.MaxHeight = double.PositiveInfinity;
    }

    private void LimitContentSizeToScreen()
    {
        Screen? screen = GetTargetScreen();
        if (screen == null) return;

        const double decorationsMargin = 40; //title bar and borders
        const double fractionOfScreen = 0.6; //Modal should be smaller than screen to feel like a modal
        ContentPresenter.MaxWidth = screen.WorkingArea.Width / screen.Scaling *  fractionOfScreen;
        ContentPresenter.MaxHeight = Math.Max(screen.WorkingArea.Height / screen.Scaling - decorationsMargin, 0) * fractionOfScreen;
    }

    /// <summary>
    /// The screen the modal will be shown on, which is the screen of the window that opens it.
    /// </summary>
    private Screen? GetTargetScreen()
    {
        TopLevel? openingWindow = Owner as TopLevel
            ?? (global::Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?
                .Windows.FirstOrDefault(window => window != this && window.IsActive);
        Screen? openingScreen = openingWindow?.Screens?.ScreenFromTopLevel(openingWindow);
        return openingScreen ?? Screens.ScreenFromTopLevel(this) ?? Screens.Primary;
    }

    private async void TopLevel_OnClosed(object? sender, EventArgs e)
    {
        try
        {
            if (_disposeOnClose && DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }
            else if (_disposeOnClose && DataContext is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            ILogger logger = ServiceResolver.Resolve<ILogger>();
            logger.Error("Failed to dispose modal viewmodel", ex);
        }
    }

    private sealed class RegisteredTemplateSelector : IDataTemplate
    {
        public bool Match(object? data) => data is IModalViewModel vm && Lookup(vm.GetType()) is not null;

        public Control? Build(object? data) =>
            data is IModalViewModel vm ? Lookup(vm.GetType())?.Invoke() : null;

        private static Func<Control>? Lookup(Type type)
        {
            //Go from specific to general, first VM type, than base class, etc
            for (Type? current = type; current is not null; current = current.BaseType)
            {
                if (_templates.TryGetValue(current, out var factory))
                    return factory;
            }
            return null;
        }
    }
}
