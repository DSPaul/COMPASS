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
        private static Browser browser;
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
            string driverName = browser switch
            {
                Browser.Chrome => "chromedriver.exe",
                Browser.Firefox => "geckodriver.exe",
                _ => "msedgedriver.exe"
            };

            string driverPath = FindFileDirectory(driverName, WebDriverDirectoryPath);

            DriverService driverService = browser switch
            {
                Browser.Chrome => ChromeDriverService.CreateDefaultService(driverPath),
                Browser.Firefox => FirefoxDriverService.CreateDefaultService(driverPath),
                _ => EdgeDriverService.CreateDefaultService(driverPath)
            };

            driverService.HideCommandPromptWindow = true;

            List<string> DriverArguments = new()
                {
                    "--headless",
                    "--window-size=3000,3000",
                    "--width=3000",
                    "--height=3000"
                };

            switch (browser)
            {
                case Browser.Chrome:
                    ChromeOptions CO = new();
                    CO.AddArguments(DriverArguments);
                    _webDriver = new ChromeDriver((ChromeDriverService)driverService, CO);
                    break;

                case Browser.Firefox:
                    FirefoxOptions FO = new();
                    FO.AddArguments(DriverArguments);
                    _webDriver = new FirefoxDriver((FirefoxDriverService)driverService, FO);
                    break;

                default:
                    EdgeOptions EO = new();
                    EO.AddArguments(DriverArguments);
                    _webDriver = new EdgeDriver((EdgeDriverService)driverService, EO);
                    break;
            }
            return _webDriver;
        }

        public static void UpdateWebdriver()
        {
            Directory.CreateDirectory(WebDriverDirectoryPath);
            DriverManager driverManager = new(WebDriverDirectoryPath);

            if (IsInstalled("chrome.exe"))
            {
                browser = Browser.Chrome;
                try
                {
                    driverManager.SetUpDriver(new ChromeConfig(), WebDriverManager.Helpers.VersionResolveStrategy.MatchingBrowser);
                }
                catch (Exception ex)
                {
                    Logger.Error("Chrome webdriver could not be initialised", ex);
                }
            }

            else if (IsInstalled("firefox.exe"))
            {
                browser = Browser.Firefox;
                try
                {
                    driverManager.SetUpDriver(new FirefoxConfig());
                }
                catch (Exception ex)
                {
                    Logger.Error("Firefox webdriver could not be initialised", ex);
                }
            }

            else
            {
                browser = Browser.Edge;
                try
                {
                    driverManager.SetUpDriver(new EdgeConfig(), WebDriverManager.Helpers.VersionResolveStrategy.MatchingBrowser);
                }
                catch (Exception ex)
                {
                    Logger.Error("Firefox webdriver could not be initialised", ex);
                }
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
            string currentUserRegistryPathPattern = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\App Paths\";
            string localMachineRegistryPathPattern = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\";

            var currentUserPath = Microsoft.Win32.Registry.GetValue(currentUserRegistryPathPattern + name, "", null);
            var localMachinePath = Microsoft.Win32.Registry.GetValue(localMachineRegistryPathPattern + name, "", null);

            if (currentUserPath != null || localMachinePath != null)
            {
                return true;
            }
            return false;
        }
    }
}
