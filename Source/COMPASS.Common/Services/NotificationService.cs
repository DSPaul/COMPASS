using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.Views.Windows;

namespace COMPASS.Common.Services
{
    public class NotificationService : INotificationService
    {
        public async Task ShowDialog(Notification notification)
        {
            var window = new NotificationWindow(notification);
            await window.ShowDialog(WindowManager.ActiveWindow);
        }
    }
}
