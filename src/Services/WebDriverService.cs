using COMPASS.Tools;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace COMPASS.Services
{
    public static class WebDriverService
    {
        private static Browser _browser;
        public enum Browser
        {
            Chrome,
            Firefox,
            Edge
        }

        //Get an initialised webdriver with right browser
        private static WebDriver? _webDriver;
        public static async Task<WebDriver> GetWebDriver()
        {
            DriverService driverService = _browser switch
            {
                Browser.Chrome => ChromeDriverService.CreateDefaultService(),
                Browser.Firefox => FirefoxDriverService.CreateDefaultService(),
                Browser.Edge => EdgeDriverService.CreateDefaultService(),
                _ => throw new System.NotImplementedException()
            };
            driverService.HideCommandPromptWindow = true;

            List<string> driverArguments = new()
                {
                    "--headless",
                    "--window-size=3000,3000",
                    "--width=3000",
                    "--height=3000"
                };

            switch (_browser)
            {
                case Browser.Chrome:
                    ChromeOptions co = new();
                    co.AddArguments(driverArguments);
                    _webDriver = await Task.Run(() => new ChromeDriver((ChromeDriverService)driverService, co));
                    break;

                case Browser.Firefox:
                    FirefoxOptions fo = new();
                    fo.AddArguments(driverArguments);
                    _webDriver = await Task.Run(() => new FirefoxDriver((FirefoxDriverService)driverService, fo));
                    break;

                default:
                    EdgeOptions eo = new();
                    eo.AddArguments(driverArguments);
                    _webDriver = await Task.Run(() => new EdgeDriver((EdgeDriverService)driverService, eo));
                    break;
            }
            return _webDriver;
        }

        public static void InitWebdriver()
        {
            if (IsInstalled("chrome.exe"))
            {
                _browser = Browser.Chrome;
                Logger.Debug("Chrome install found");
            }
            else if (IsInstalled("firefox.exe"))
            {
                _browser = Browser.Firefox;
                Logger.Debug("firefox install found");
            }
            else if (IsInstalled("msedge.exe"))
            {
                _browser = Browser.Edge;
                Logger.Debug("edge install found");
            }
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
