using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Threading;
using Avalonia.VisualTree;
using COMPASS.Infra.Avalonia.Mvvm;
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

        // Wait until the layout is ready
        ContentPresenter.Loaded += (_, _) => UpdateSizeBounds();
    }

    private readonly bool _disposeOnClose;

    private void UpdateSizeBounds()
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
