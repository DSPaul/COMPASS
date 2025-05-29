using System;
using System.IO;

namespace COMPASS.Common.Interfaces.Services
{
    public interface IEnvironmentVarsService
    {
        //vars
        const string ENV_KEY_CompassDataPath = "COMPASS_DATA_PATH";

        public static readonly string DefaultDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "COMPASS");

        string CompassDataPath { get; set; }
    }
}
