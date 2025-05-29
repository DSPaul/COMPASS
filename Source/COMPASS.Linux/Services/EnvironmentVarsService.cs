using System.Diagnostics;
using System.IO;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Interfaces.Services;

namespace COMPASS.Linux.Services
{
    public class EnvironmentVarsService : IEnvironmentVarsService
    {
        //from https://nextcodeblock.com/posts/handling-environments-variable-in-linux-with-csharp/
        public string CompassDataPath
        {
            get
            {
                string? pathInEnv = null;
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = $"-c \"echo ${IEnvironmentVarsService.ENV_KEY_CompassDataPath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                proc.Start();
                while (!proc.StandardOutput.EndOfStream)
                    pathInEnv = proc.StandardOutput.ReadLine();

                proc.WaitForExit();
                return Path.Exists(pathInEnv) ? pathInEnv : IEnvironmentVarsService.DefaultDataPath;
            }

            set
            {
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = $"-c \"echo export {IEnvironmentVarsService.DefaultDataPath}={value}>>~/.bashrc; source ~/.bashrc\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                proc.Start();
                proc.WaitForExit();
            }
        }
    }
}
