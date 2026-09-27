using Avalonia.Threading;
using COMPASS.Common.Views.Windows;
using COMPASS.Infra.Avalonia.Modal;
using COMPASS.Infra.Notifications;

namespace COMPASS.Common.Services
{
    public class NotificationService : INotificationService
    {
        public Task ShowDialog(Notification notification)
        {
            return Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var window = new NotificationWindow(notification);
                await window.ShowDialog(WindowManager.ActiveWindow);
            });
        }
    }
}
