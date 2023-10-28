using COMPASS.ViewModels;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

namespace COMPASS.Tools
{
    public static class WebDriverFactory
    {
        private static readonly string WebDriverDirectoryPath = Path.Combine(SettingsViewModel.CompassDataPath, "WebDrivers");
        private static Browser _browser;
        public enum Browser
        {
            Chrome,
            Firefox,
            Edge
        }

        //Get an initialised webdriver with right browser
        private static WebDriver _webDriver;
        public static WebDriver GetWebDriver()
        {
            string driverName = _browser switch
            {
                Browser.Chrome => "chromedriver.exe",
                Browser.Firefox => "geckodriver.exe",
                _ => "msedgedriver.exe"
            };

            string driverPath = FindFileDirectory(driverName, WebDriverDirectoryPath);

            DriverService driverService = _browser switch
            {
                Browser.Chrome => ChromeDriverService.CreateDefaultService(driverPath),
                Browser.Firefox => FirefoxDriverService.CreateDefaultService(driverPath),
                _ => EdgeDriverService.CreateDefaultService(driverPath)
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
                    _webDriver = new ChromeDriver((ChromeDriverService)driverService, co);
                    break;

                case Browser.Firefox:
                    FirefoxOptions fo = new();
                    fo.AddArguments(driverArguments);
                    _webDriver = new FirefoxDriver((FirefoxDriverService)driverService, fo);
                    break;

                default:
                    EdgeOptions eo = new();
                    eo.AddArguments(driverArguments);
                    _webDriver = new EdgeDriver((EdgeDriverService)driverService, eo);
                    break;
            }
            return _webDriver;
        }

        public static void UpdateWebdriver()
        {
            Directory.CreateDirectory(WebDriverDirectoryPath);
            DriverManager driverManager = new(WebDriverDirectoryPath);

            bool success = false;
            Exception exception = null;

            if (IsInstalled("chrome.exe"))
            {
                _browser = Browser.Chrome;
                try
                {
                    driverManager.SetUpDriver(new ChromeConfig(), WebDriverManager.Helpers.VersionResolveStrategy.MatchingBrowser);
                    success = true;
                }
                catch (Exception ex)
                {
                    exception = ex;
                    Logger.Warn("Chrome webdriver could not be initialised", ex);
                }
            }

            if (!success && IsInstalled("firefox.exe"))
            {
                _browser = Browser.Firefox;
                try
                {
                    driverManager.SetUpDriver(new FirefoxConfig());
                    success = true;
                }
                catch (Exception ex)
                {
                    exception = ex;
                    Logger.Warn("Firefox webdriver could not be initialised", ex);
                }
            }

            if (!success && IsInstalled("msedge.exe"))
            {
                _browser = Browser.Edge;
                try
                {
                    driverManager.SetUpDriver(new EdgeConfig(), WebDriverManager.Helpers.VersionResolveStrategy.MatchingBrowser);
                    success = true;
                }
                catch (Exception ex)
                {
                    exception = ex;
                    Logger.Warn("Edge webdriver could not be initialised", ex);
                }
            }

            if (!success)
            {
                Logger.Error("No webdriver could be initialised", exception);
            }
            else
            {
                Logger.Info($"Successfully initialised {_browser} webdriver");
            }
        }

        private static string FindFileDirectory(string fileName, string rootDirectory)
        {
            string filePath = Directory.GetFiles(rootDirectory, fileName, SearchOption.AllDirectories).Last();
            string parentDirectory = Path.GetDirectoryName(filePath);
            return parentDirectory;
        }

        //helper function to check if certain browsers are installed
        private static bool IsInstalled(string name)
        {
            const string currentUserRegistryPathPattern = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\App Paths\";
            const string localMachineRegistryPathPattern = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\";

            var currentUserPath = Microsoft.Win32.Registry.GetValue(currentUserRegistryPathPattern + name, "", null)?.ToString();
            var localMachinePath = Microsoft.Win32.Registry.GetValue(localMachineRegistryPathPattern + name, "", null)?.ToString();

            return currentUserPath != null && Path.Exists(currentUserPath) ||
                  localMachinePath != null && Path.Exists(localMachinePath);
        }
    }
}
