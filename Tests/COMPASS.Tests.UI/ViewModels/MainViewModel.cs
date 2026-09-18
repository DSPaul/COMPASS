using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Services;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.Views.Windows;
using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Enums;
using COMPASS.Infra.Models.Measuring;
using COMPASS.Infra.Models.Progress;

namespace COMPASS.Tests.UI.ViewModels;

public class MainViewModel
{
    public AsyncRelayCommand OpenProgressWindowCommand => field ??= new(OpenProgressWindow);
    public AsyncRelayCommand OpenInfoWindowCommand => field ??= new(OpenInfoWindow);
    public AsyncRelayCommand OpenWarningWindowCommand => field ??= new(OpenWarningWindow);
    public AsyncRelayCommand OpenErrorWindowCommand => field ??= new(OpenErrorWindow);
    public RelayCommand OpenLoadingWindowCommand => field ??= new(OpenLoadingWindow);

    private static async Task OpenProgressWindow()
    {
        const int iterations = 100;
        ProgressTracker progressTracker = new(Quantities.Items())
        {
            StatusMessage = "Doing stuff",
            Total = iterations
        };

        using ProgressTrackingManager trackingManager = new();
        TrackedOperation demoOperation = trackingManager.Track(progressTracker, "Doing stuff");
        ProgressWindow demoWindow = new(demoOperation);
        demoWindow.Show();

        try
        {
            await Task.Run(() =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    Thread.Sleep(50);
                    progressTracker.Report(ProgressReports.Log(new LogEntry(Severity.Info, $"Iteration {i}")));
                    progressTracker.Report(ProgressReports.Increment);
                }
            });
        }
        finally
        {
            trackingManager.Untrack(demoOperation);
            demoOperation.Dispose();
            await Dispatcher.UIThread.InvokeAsync(demoWindow.Close);
        }
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