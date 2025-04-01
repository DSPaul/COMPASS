using Autofac;
using COMPASS.Common.Interfaces;
using System.IO;
using System.Text.RegularExpressions;

namespace COMPASS.Common.Models
{
    public static partial class Constants
    {
        public const string RepoURL = "https://github.com/DSPAUL/COMPASS";
        public const string LinkTreeURL = "https://linktr.ee/compassapp";

        public const string SatchelExtension = ".satchel";

        //File names
        public const string SatchelInfoFileName = "SatchelInfo.json";
        

        public static string InstallersPath => Path.Combine(App.Container.Resolve<IEnvironmentVarsService>().CompassDataPath, "Installers");
        public const string AutoUpdateXMLPath = "https://raw.githubusercontent.com/DSPAUL/COMPASS/master/versionInfo.xml";

        //Regex expresions
        [GeneratedRegex(@"(978|979)[- ]?\d{1,5}[- ]?\d{1,7}[- ]?\d{1,6}[- ]?\d")]
        public static partial Regex RegexISBN();

        [GeneratedRegex(@"\s+")]
        public static partial Regex RegexWhitespace();


        [GeneratedRegex(@"\d+")]
        public static partial Regex RegexNumbersOnly();
        
        //Command line arguments
        public const string CmdArgNotifyCrashed = "notify_crashed";
    }
}
