using System;

namespace COMPASS.Models.Enums
{
    [Flags]
    public enum NotificationAction
    {
        /// <summary>
        /// Continues the operation
        /// </summary>
        Confirm = 1,
        /// <summary>
        /// Breaks of the operations
        /// </summary>
        Cancel = 2,
        /// <summary>
        /// Continues with with some option refused
        /// </summary>
        Decline = 4
    }
}
