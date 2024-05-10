using COMPASS.Common.Interfaces;
using COMPASS.Common.Models;

namespace COMPASS.Common.Services
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
