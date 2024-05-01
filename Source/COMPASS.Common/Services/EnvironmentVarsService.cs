using System;
using System.IO;

namespace COMPASS.Common.Services
{
    public static class EnvironmentVarsService
    {
        //vars
        const string ENV_KEY_CompassDataPath = "COMPASS_DATA_PATH";

        public static string DefaultDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "COMPASS");
        public static string CompassDataPath
        {
            get => Environment.GetEnvironmentVariable(ENV_KEY_CompassDataPath) ?? DefaultDataPath;
            set => Environment.SetEnvironmentVariable(ENV_KEY_CompassDataPath, value, EnvironmentVariableTarget.User); //This only works on windows, so set up DI for this
        }
    }
}
