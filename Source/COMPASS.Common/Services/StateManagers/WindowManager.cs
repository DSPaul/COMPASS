using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using COMPASS.Common.Interfaces.ViewModels;
using COMPASS.Common.Views.Windows;

namespace COMPASS.Common.Services.StateManagers;

public static class WindowManager
{
    public static Window MainWindow { get; set; } = null!;

    public static Window ActiveWindow => (Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)!.Windows.FirstOrDefault(w => w.IsActive) ?? MainWindow;
    
    public static async Task OpenModal(IModalViewModel modalViewModel)
    {
        ModalWindow modalWindow = new(modalViewModel);
        await modalWindow.ShowDialog(ActiveWindow);
    }
}