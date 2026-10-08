using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using COMPASS.Infra.DependencyInjection;
using COMPASS.Infra.Web;

namespace COMPASS.Infra.Avalonia.Modal;

public static class WindowManager
{
    public static Window MainWindow 
    { 
        get;
        set
        {
            if(field != value)
            {
                if(field != null)
                {
                    field.Activated -= OnWindowActivated;
                    field.Deactivated -= OnWindowDeactivated;
                }

                field = value;

                field.Activated += OnWindowActivated;
                field.Deactivated += OnWindowDeactivated;
            }
        }
    } = null!;

    public static Window ActiveWindow => (global::Avalonia.Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)!.Windows.FirstOrDefault(w => w.IsActive) ?? MainWindow;
    
    public static async Task OpenModal(IModalViewModel modalViewModel)
    {
        ModalWindow modalWindow = new(modalViewModel);
        await modalWindow.ShowDialog(ActiveWindow);
    }

    private static void OnWindowActivated(object? sender, EventArgs e)
    {
        var connectivityManager = ServiceResolver.Resolve<ConnectivityManager>();
        connectivityManager.Resume();
    }

    private static void OnWindowDeactivated(object? sender, EventArgs e)
    {
        var connectivityManager = ServiceResolver.Resolve<ConnectivityManager>();
        connectivityManager.Pause();
    }
}