using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.ViewModels;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.Views.Windows;

public partial class ModalWindow : Window
{
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
}