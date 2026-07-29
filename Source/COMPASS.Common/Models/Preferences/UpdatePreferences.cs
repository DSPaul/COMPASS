using System;
using System.Collections.Generic;
using System.Text;

namespace COMPASS.Common.Models.Preferences
{
    public class UpdatePreferences
    {
        public bool CheckForUpdates { get; set; } = true;
        public bool IncludePrerelease { get; set; } = false;
        public HashSet<string> SkippedUpdates { get; set; } = [];
    }
}
