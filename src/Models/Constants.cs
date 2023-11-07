using COMPASS.ViewModels;
using System.IO;
using System.Text.RegularExpressions;

namespace COMPASS.Models
{
    public static partial class Constants
    {
        public const string RepoURL = "https://github.com/DSPAUL/COMPASS";

        public const string COMPASSFileExtension = ".cmpss";

        public static string InstallersPath => Path.Combine(SettingsViewModel.CompassDataPath, "Installers");
        public const string AutoUpdateXMLPath = "https://raw.githubusercontent.com/DSPAUL/COMPASS/master/versionInfo.xml";

        [GeneratedRegex("(978|979)[- ]?\\d{1,5}[- ]?\\d{1,7}[- ]?\\d{1,6}[- ]?\\d")]
        public static partial Regex RegexISBN();

        [GeneratedRegex("\\s+")]
        public static partial Regex RegexWhitespace();


        [GeneratedRegex("[0-9]+")]
        public static partial Regex RegexNumbersOnly();
    }
}
