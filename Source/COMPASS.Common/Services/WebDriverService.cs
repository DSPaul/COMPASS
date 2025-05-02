using System.Collections.Generic;
using System.Threading.Tasks;
using COMPASS.Common.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Safari;

namespace COMPASS.Common.Services
{
    public abstract class WebDriverServiceBase : IWebDriverService
    {
        protected Browser _browser;
        protected enum Browser
        {
            /// <summary>
            /// Not searched yet
            /// </summary>
            Unknown = 0,
            /// <summary>
            /// Searched and found nothing
            /// </summary>
            None,
            Chrome,
            Chromium,
            Firefox,
            Edge,
            Safari
        }

        protected abstract Browser DetectInstalledBrowser();

        //Get an initialised webdriver with right browser
        protected WebDriver? _webDriver;
        public async Task<WebDriver?> GetWebDriver()
        {
            if (_browser == Browser.Unknown)
            {
                _browser = DetectInstalledBrowser();
            }

            DriverService? driverService = _browser switch
            {
                Browser.Chrome => ChromeDriverService.CreateDefaultService(),
                Browser.Firefox => FirefoxDriverService.CreateDefaultService(),
                Browser.Edge => EdgeDriverService.CreateDefaultService(),
                Browser.Safari => SafariDriverService.CreateDefaultService(),
                _ => null //not a supported browser
            };

            //No supported driver found
            if (driverService == null) return null;

            driverService.HideCommandPromptWindow = true;

            List<string> driverArguments =
            [
                "--headless",
                "--window-size=3000,3000",
                "--width=3000",
                "--height=3000"
            ];

            switch (_browser)
            {
                case Browser.Chrome:
                    ChromeOptions co = new();
                    co.AddArguments(driverArguments);
                    List<string> chromeArgs = new()
                    {
                        "--disable-search-engine-choice-screen",
                        "--disable-features=OptimizationGuideModelDownloading,OptimizationHintsFetching,OptimizationTargetPrediction,OptimizationHints"
                    };

                    co.AddArguments(chromeArgs);

                    _webDriver = await Task.Run(() => new ChromeDriver((ChromeDriverService)driverService, co));
                    break;

                case Browser.Firefox:
                    FirefoxOptions fo = new();
                    fo.AddArguments(driverArguments);
                    _webDriver = await Task.Run(() => new FirefoxDriver((FirefoxDriverService)driverService, fo));
                    break;

                case Browser.Edge:
                    EdgeOptions eo = new();
                    eo.AddArguments(driverArguments);
                    _webDriver = await Task.Run(() => new EdgeDriver((EdgeDriverService)driverService, eo));
                    break;

                case Browser.Safari:
                    SafariOptions so = new();
                    //so.AddArguments(driverArguments); //method doesn't exist for safari
                    _webDriver = await Task.Run(() => new SafariDriver((SafariDriverService)driverService, so));
                    break;
            }
            return _webDriver;
        }
    }
}
