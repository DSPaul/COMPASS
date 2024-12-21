using System.Threading.Tasks;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Models;

namespace COMPASS.Common.Services
{
    public class WindowedNotificationService : INotificationService
    {
        public async void Show(Notification notification)
        {
            var window = new NotificationWindow(notification);
            await window.ShowDialog(App.MainWindow);
        }
    }
}
