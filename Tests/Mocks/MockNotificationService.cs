using COMPASS.Interfaces;
using COMPASS.Models;
using COMPASS.Models.Enums;

namespace Tests.Mocks
{
    internal class MockNotificationService : INotificationService
    {
        public void Show(Notification notification)
        {
            Console.WriteLine(notification.Body);
            notification.Result = NotificationAction.Confirm;
        }
    }
}
