using Avalonia.Threading;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.Views.Windows;
using COMPASS.Infra.Interfaces.Services;
using COMPASS.Infra.Models;

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
