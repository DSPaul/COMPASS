using COMPASS.Common.Interfaces;
using System;
using System.IO;

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
