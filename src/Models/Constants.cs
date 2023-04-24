using COMPASS.ViewModels;
using System.IO;
using System.Text.RegularExpressions;

namespace COMPASS.Models
{
    public static partial class Constants
    {
        public const string RepoURL = "https://github.com/DSPAUL/COMPASS";

        public static string InstallersPath => Path.Combine(SettingsViewModel.CompassDataPath, "Installers");
        public static readonly string AutoUpdateXMLPath = "https://raw.githubusercontent.com/DSPAUL/COMPASS/master/versionInfo.xml";

        [GeneratedRegex("ISBN[:]*[0-9]+[-]+[0-9]+[-]+[0-9]+[-]+[0-9]+[-]+[0-9]+")]
        public static partial Regex RegexISBN();

        [GeneratedRegex("\\s+")]
        public static partial Regex RegexWhitespace();

        [GeneratedRegex("[0-9]+[-]+[0-9]+[-]+[0-9]+[-]+[0-9]+[-]+[0-9]+")]
        public static partial Regex RegexISBNNumberOnly();


        [GeneratedRegex("[0-9]+")]
        public static partial Regex RegexNumbersOnly();
    }
}
