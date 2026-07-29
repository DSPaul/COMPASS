using COMPASS.Common.Interfaces.Storage;

namespace COMPASS.Common.Models
{
    public static partial class Constants
    {
        public const string RepoName = "DSPAUL/COMPASS";
        public const string LinkTreeURL = "https://linktr.ee/compassapp";

        public const string SatchelExtension = ".satchel";

        //File names
        public const string DIR_ROOT = "COMPASS";
        public const string DIR_COLLECTIONS = "Collections";
        public const string DIR_THUMBNAILS = "Thumbnails";
        public const string DIR_COVERS = "CoverArt";
        public const string DIR_LOGS = "logs";
        public const string DIR_UPDATES = "updates";

        public const string DEFAULT_COLLECTION_NAME = "Default Collection";

        public const string SatchelInfoFileName = "SatchelInfo.json";

        public static string InstallersPath => Path.Combine(IApplicationDataService.ApplicationDataPath, "Installers");
        public const string AutoUpdateXMLPath = "https://raw.githubusercontent.com/DSPAUL/COMPASS/master/versionInfo.xml";
        
        //Command line arguments
        public const string CmdArgNotifyCrashed = "notify_crashed";
    }
}