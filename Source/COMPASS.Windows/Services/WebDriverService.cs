using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Services;

namespace COMPASS.Windows.Services
{
    class WebDriverService(ILogger logger) : WebDriverServiceBase
    {
        protected override Browser DetectInstalledBrowser()
        {
            if (IsInstalled("chrome.exe"))
            {
                logger.Debug("Chrome install found");
                return Browser.Chrome;
            }
            else if (IsInstalled("firefox.exe"))
            {
                logger.Debug("firefox install found");
                return Browser.Firefox;
            }
            else if (IsInstalled("msedge.exe"))
            {
                logger.Debug("edge install found");
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
