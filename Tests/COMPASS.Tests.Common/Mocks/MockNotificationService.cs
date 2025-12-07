using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;

namespace COMPASS.Tests.Common.Mocks;

public class MockNotificationService : INotificationService
{
    public void Show(Notification notification)
    {
        Console.WriteLine(notification.Body);
        notification.Result = NotificationAction.Confirm;
    }

    public Task ShowDialog(Notification notification)
    {
        Console.WriteLine(notification.Body);
        notification.Result = NotificationAction.Confirm;
        return Task.CompletedTask;
    }
}