using Avalonia.Rendering;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels;
using COMPASS.Common.Views.Windows;
using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Enums;

namespace COMPASS.Tests.UI.ViewModels;

public class MainViewModel
{
    public RelayCommand OpenProgressWindowCommand => field ??= new(OpenProgressWindow);
    public AsyncRelayCommand OpenInfoWindowCommand => field ??= new(OpenInfoWindow);
    public AsyncRelayCommand OpenWarningWindowCommand => field ??= new(OpenWarningWindow);
    public AsyncRelayCommand OpenErrorWindowCommand => field ??= new(OpenErrorWindow);
    public RelayCommand OpenLoadingWindowCommand => field ??= new(OpenLoadingWindow);

    private static void OpenProgressWindow()
    {
        var progressVm = ProgressViewModel.GetInstance();
        int iterations = 100;
        
        progressVm.Clear();
        progressVm.TotalAmount = iterations;
        progressVm.Text = "Doing stuff";
        
        ProgressWindow w = new();
        w.Show();

        Task.Run(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                Thread.Sleep(50);
                LogEntry entry = new(Severity.Info, $"Iteration {i}");
                progressVm.AddLogEntry(entry);
                progressVm.IncrementCounter();
            }
        });
    }

    private static async Task OpenInfoWindow()
    {
        var notificationService = new NotificationService();
        var notification = new Notification("A test notification", "This notification is display for testing purposes");
        await notificationService.ShowDialog(notification);
    }
    
    private static async Task OpenWarningWindow()
    {
        var notificationService = new NotificationService();
        var notification = new Notification("A test notification", "This notification is display for testing purposes", Severity.Warning);
        await notificationService.ShowDialog(notification);
    }
    
    private static async Task OpenErrorWindow()
    {
        var notificationService = new NotificationService();
        var notification = new Notification("A test notification", "This notification is display for testing purposes", Severity.Error);
        await notificationService.ShowDialog(notification);
    }

    private static void OpenLoadingWindow()
    {
        var loadingWindow = new LoadingWindow("Doing stuff, please hold on");
        loadingWindow.Show();
        
        Task.Run(() =>
        {
            Thread.Sleep(3000);
            Dispatcher.UIThread.Invoke(() => loadingWindow.Close());
        });
    }
}