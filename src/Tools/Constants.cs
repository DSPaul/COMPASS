using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COMPASS.Tools
{
    public static  class Constants
    {
        public static readonly string RepoURL = "https://github.com/DSPAUL/COMPASS";
        public static readonly string CompassDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"COMPASS");
        public static readonly string PreferencesFilePath = Path.Combine(CompassDataPath + "Preferences.xml");
    }
}
