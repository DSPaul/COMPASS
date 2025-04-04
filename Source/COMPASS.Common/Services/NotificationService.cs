using System.Threading.Tasks;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Models;
using COMPASS.Common.Views.Windows;

namespace COMPASS.Common.Services
{
    public class NotificationService : INotificationService
    {
        public async Task ShowDialog(Notification notification)
        {
            var window = new NotificationWindow(notification);
            await window.ShowDialog(App.MainWindow);
        }
    }
}
