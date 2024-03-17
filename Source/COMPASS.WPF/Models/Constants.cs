using COMPASS.ViewModels;
using System.IO;
using System.Text.RegularExpressions;

namespace COMPASS.Models
{
    public static partial class Constants
    {
        public const string RepoURL = "https://github.com/DSPAUL/COMPASS";

        public const string SatchelExtension = ".satchel";
        public const string SatchelExtensionFilter = $"COMPASS Satchel File (*{SatchelExtension})|*{SatchelExtension}";

        //File names
        public const string SatchelInfoFileName = "SatchelInfo.json";
        public const string CodicesFileName = "CodexInfo.xml";
        public const string TagsFileName = "Tags.xml";
        public const string CollectionInfoFileName = "CollectionInfo.xml";

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
