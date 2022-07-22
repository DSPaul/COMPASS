using System;
using System.IO;

namespace COMPASS.Models
{
    public static class Constants
    {
        public const string RepoURL = "https://github.com/DSPAUL/COMPASS";
        public static readonly string CompassDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "COMPASS");
        public static readonly string PreferencesFilePath = Path.Combine(CompassDataPath,"Preferences.xml");
        public static readonly string WebDriverDirectoryPath = Path.Combine(CompassDataPath,"WebDrivers");
        public static readonly string InstallersPath = Path.Combine(CompassDataPath,"Installers");
    }
}
