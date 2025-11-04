using System.IO;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces.Services;

namespace COMPASS.Common.Models
{
    public static partial class Constants
    {
        public const string RepoURL = "https://github.com/DSPAUL/COMPASS";
        public const string LinkTreeURL = "https://linktr.ee/compassapp";

        public const string SatchelExtension = ".satchel";

        //File names
        public const string SatchelInfoFileName = "SatchelInfo.json";


        public static string InstallersPath => Path.Combine(ServiceResolver.Resolve<IEnvironmentVarsService>().CompassDataPath, "Installers");
        public const string AutoUpdateXMLPath = "https://raw.githubusercontent.com/DSPAUL/COMPASS/master/versionInfo.xml";
        
        //Command line arguments
        public const string CmdArgNotifyCrashed = "notify_crashed";
    }
}