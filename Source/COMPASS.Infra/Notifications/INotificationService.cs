using System.Diagnostics;

namespace COMPASS.Infra.Notifications
{
    public interface INotificationService
    {
        /// <summary>
        /// Use when the dialog result is needed.
        /// </summary>
        Task ShowDialog(Notification notification);
    }

    public static class NotificationServiceExtensions
    {
        /// <summary>
        /// Use for one-way error/info notifications where the result is irrelevant.
        /// </summary>
        extension(INotificationService service)
        {
            public void Notify(Notification notification)
            {
                //Should only be confirmable, if more actions are needed, use async version instead and handle the result.
                Debug.Assert(notification.Actions == NotificationAction.Confirm);

                _ = service.ShowDialog(notification);
            }
        }
    }
}
