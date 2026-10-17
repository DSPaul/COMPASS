using COMPASS.Infra.Preferences;

namespace COMPASS.Infra.Updates
{
    public class UpdatePreferences : IPreferences
    {
        public bool CheckForUpdates { get; set; } = true;
        public bool IncludePrerelease { get; set; } = false;

        /// <summary>
        /// Updates that the user has been notified about
        /// </summary>
        public HashSet<string> NotifiedUpdates { get; set; } = new HashSet<string>();
    }
}
