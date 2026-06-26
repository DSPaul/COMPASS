using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using COMPASS.Common.Interfaces.ViewModels;
using COMPASS.Common.Views.Windows;

namespace COMPASS.Common.Services.StateManagers;

public static class WindowManager
{
    private static Window _mainWindow = null!;
    private static readonly HashSet<Window> _closingDialogs = [];
    public static Window MainWindow
    {
        get => _mainWindow;
        set
        {
            if (_mainWindow != null)
                _mainWindow.Activated -= OnMainWindowActivated;
            _mainWindow = value;
            if (_mainWindow != null)
                _mainWindow.Activated += OnMainWindowActivated;
        }
    }

    private static IReadOnlyList<Window> AllWindows => (Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)!.Windows;
    public static Window ActiveWindow => AllWindows.FirstOrDefault(w => w.IsActive) ?? MainWindow;

    public static async Task ShowDialog(Window dialog)
    {
        // Guard against closing dialogs: the OS activates the main window before the dialog is fully
        // gone, which would cause a flicker if we re-activated the closing window.
        dialog.Closing += RegisterClosingDialog;
        dialog.Closed += UnRegisterClosingDialog;

        await dialog.ShowDialog(ActiveWindow);
    }

    public static async Task OpenModal(IModalViewModel modalViewModel)
    {
        ModalWindow modalWindow = new(modalViewModel);
        await ShowDialog(modalWindow);
    }

    private static void RegisterClosingDialog(object? sender, EventArgs e)
    {
        if (sender is Window dialog)
        {
            _closingDialogs.Add(dialog);
        }
    }

    private static void UnRegisterClosingDialog(object? sender, EventArgs e)
    {
        if (sender is Window dialog)
        {
            _closingDialogs.Remove(dialog);
            dialog.Closing -= RegisterClosingDialog;
            dialog.Closed -= UnRegisterClosingDialog;
        }
    }


    // On Linux, clicking the taskbar to switch back to the app sends the activation request to the
    // main window, but a modal dialog blocks it so the user can't interact with the app.
    // Re-direct activation to the topmost open dialog so it receives focus instead.

    private static void OnMainWindowActivated(object? sender, EventArgs e)
    {
        Window? topmostDialog = AllWindows.LastOrDefault(w => w.IsVisible && w.IsDialog && !_closingDialogs.Contains(w));
        topmostDialog?.Activate();
    }
}