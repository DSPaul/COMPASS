using System.Diagnostics;
using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Enums;

namespace COMPASS.Infra.Interfaces.Services
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
        public static void Notify(this INotificationService service, Notification notification)
        {
            //Should only be confirmable, if more actions are needed, use async version instead and handle the result.
            Debug.Assert(notification.Actions == NotificationAction.Confirm);

            _ = service.ShowDialog(notification);
        }
    }
}
