using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Common.Models.Enums;

namespace COMPASS.Common.Models
{
    public class Notification : ObservableObject
    {
        public Notification(string title, string? body = null, Severity severity = Severity.Info, NotificationAction actions = NotificationAction.Confirm)
        {
            Title = title;
            Body = body ?? title;
            Severity = severity;
            Actions = actions;
        }

        public string Title { get; set; }
        public string Body { get; set; }

        /// <summary>
        /// For bigger text dumps, is shown in a collapsable, scrollable control
        /// </summary>
        public string Details { get; set; } = "";

        public string ConfirmText { get; set; } = "Ok";
        public string CancelText { get; set; } = "Cancel";
        public string DeclineText { get; set; } = "No";

        public Severity Severity { get; set; }

        public NotificationAction Actions { get; set; }
        public NotificationAction Result { get; set; }

        public List<ObservableKeyValuePair<string, bool>> Options { get; set; } = [];

        #region Templates
        public static Notification AreYouSureNotification => new("Are you Sure?", severity: Severity.Warning, actions: NotificationAction.Cancel | NotificationAction.Confirm)
        {
            ConfirmText = "Yes",
            CancelText = "No"
        };
        #endregion

        public bool IsOptionSelected(string option) => Options.Single(kv => kv.Key == option).Value;
    }
}
