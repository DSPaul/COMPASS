using COMPASS.Common.Services;
using COMPASS.Common.Tools;
using System.IO;

namespace COMPASS.Windows.Services
{
    class WebDriverService : WebDriverServiceBase
    {
        protected override Browser DetectInstalledBrowser()
        {
            if (IsInstalled("chrome.exe"))
            {
                Logger.Debug("Chrome install found");
                return Browser.Chrome;
            }
            else if (IsInstalled("firefox.exe"))
            {
                Logger.Debug("firefox install found");
                return Browser.Firefox;
            }
            else if (IsInstalled("msedge.exe"))
            {
                Logger.Debug("edge install found");
                return Browser.Edge;
            }

            return Browser.None;
        }

        //helper function to check if certain browsers are installed
        private static bool IsInstalled(string name)
        {
            const string currentUserRegistryPathPattern = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\App Paths\";
            const string localMachineRegistryPathPattern = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\";

            var currentUserPath = Microsoft.Win32.Registry.GetValue(currentUserRegistryPathPattern + name, "", null)?.ToString();
            var localMachinePath = Microsoft.Win32.Registry.GetValue(localMachineRegistryPathPattern + name, "", null)?.ToString();

            return (currentUserPath != null && Path.Exists(currentUserPath)) ||
                  (localMachinePath != null && Path.Exists(localMachinePath));
        }
    }
}
