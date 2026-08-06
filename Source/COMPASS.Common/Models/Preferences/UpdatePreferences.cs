using System;
using System.Collections.Generic;
using System.Text;

namespace COMPASS.Common.Models.Preferences
{
    public class UpdatePreferences
    {
        public bool CheckForUpdates { get; set; } = true;
        public bool IncludePrerelease { get; set; } = false;

        /// <summary>
        /// Updates that the user has been notified about
        /// </summary>
        public HashSet<string> NotifiedUpdates { get; set; } = new HashSet<string>();
    }
}
