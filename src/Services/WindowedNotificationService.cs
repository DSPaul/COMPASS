using COMPASS.Interfaces;
using COMPASS.Models;
using COMPASS.Windows;

namespace COMPASS.Services
{
    public class WindowedNotificationService : INotificationService
    {
        public void Show(Notification notification)
        {
            var window = new NotificationWindow(notification);
            _ = window.ShowDialog();
        }
    }
}
