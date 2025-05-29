using System;
using System.IO;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Interfaces.Services;

namespace COMPASS.Windows.Services
{
    public class EnvironmentVarsService : IEnvironmentVarsService
    {
        public string CompassDataPath
        {
            get
            {
                string? pathInEnv = Environment.GetEnvironmentVariable(IEnvironmentVarsService.ENV_KEY_CompassDataPath);
                return Path.Exists(pathInEnv) ? pathInEnv : IEnvironmentVarsService.DefaultDataPath;
            }

            //This is windows only code
            set => Environment.SetEnvironmentVariable(IEnvironmentVarsService.ENV_KEY_CompassDataPath, value, EnvironmentVariableTarget.User);
        }
    }
}
