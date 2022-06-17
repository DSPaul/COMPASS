using System;
using System.IO;

namespace COMPASS.Tools
{
    public static class Constants
    {
        public const string RepoURL = "https://github.com/DSPAUL/COMPASS";
        public static readonly string CompassDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"COMPASS");
        public static readonly string PreferencesFilePath = Path.Combine(CompassDataPath + "\\Preferences.xml");
    }
}
