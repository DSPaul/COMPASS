using COMPASS.Common.Interfaces.Services;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Manager;
using OpenQA.Selenium.Safari;

namespace COMPASS.Common.Services
{
    public class WebDriverService(ILogger logger) : IWebDriverService
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
            Firefox,
            Edge,
            Safari
        }

        public async Task<WebDriver?> GetWebDriver()
        {
            if (_browser == Browser.Unknown)
            {
                _browser = await GetPreferredBrowser();
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

            WebDriver? webDriver = null;
            switch (driverService)
            {
                case ChromeDriverService chromeDriverService:
                    ChromeOptions co = new();
                    co.AddArguments(driverArguments);
                    List<string> chromeArgs = new()
                    {
                        "--disable-search-engine-choice-screen",
                        "--disable-features=OptimizationGuideModelDownloading,OptimizationHintsFetching,OptimizationTargetPrediction,OptimizationHints"
                    };

                    co.AddArguments(chromeArgs);

                    webDriver = await Task.Run(() => new ChromeDriver(chromeDriverService, co));
                    break;

                case FirefoxDriverService firefoxDriverService:
                    FirefoxOptions fo = new();
                    fo.AddArguments(driverArguments);
                    webDriver = await Task.Run(() => new FirefoxDriver(firefoxDriverService, fo));
                    break;

                case EdgeDriverService edgeDriverService:
                    EdgeOptions eo = new();
                    eo.AddArguments(driverArguments);
                    webDriver = await Task.Run(() => new EdgeDriver(edgeDriverService, eo));
                    break;

                case SafariDriverService safariDriverService:
                    SafariOptions so = new();
                    //so.AddArguments(driverArguments); //method doesn't exist for safari
                    webDriver = await Task.Run(() => new SafariDriver(safariDriverService, so));
                    break;
            }

            return webDriver;
        }

        private async Task<Browser> GetPreferredBrowser()
        {
            var browsersInPreferenceOrder = new List<Browser>
            {
                Browser.Chrome,
                Browser.Edge,
                Browser.Firefox,
                Browser.Safari,
            };

            foreach (var browser in browsersInPreferenceOrder)
            {
                var result = await SeleniumManager.DiscoverBrowserAsync(browser.ToString().ToLower());
                if (!string.IsNullOrEmpty(result.BrowserPath))
                {
                    logger.Debug($"Preferred browser found: {browser}");
                    return browser;
                }
            }
            logger.Debug($"No installed browsers found");
            return Browser.None;
        }
    }
}
