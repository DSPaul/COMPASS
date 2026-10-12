using COMPASS.Infra.Logging;
using ImageMagick;
using ImageMagick.Factories;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Manager;
using OpenQA.Selenium.Safari;

namespace COMPASS.Infra.Web
{
    internal class WebDriverService(ILogger logger) : IWebDriverService
    {
        const int _headlessWindowWidth = 3000;
        const int _headlessWindowHeight = 3000;

        protected Browser _browser;
        protected string _browserPath = "";
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

        public async Task<WebDriver?> GetWebDriver(CancellationToken cancellationToken = default)
        {
            if (_browser == Browser.Unknown)
            {
                await GetPreferredBrowser();
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
                $"--window-size={_headlessWindowWidth},{_headlessWindowHeight}",
                $"--width={_headlessWindowWidth}",
                $"--height={_headlessWindowHeight}"
            ];

            WebDriver? webDriver = null;
            try
            {
                switch (driverService)
                {
                    case ChromeDriverService chromeDriverService:
                        ChromeOptions co = new();
                        co.AddArguments(driverArguments);
                        if (!string.IsNullOrEmpty(_browserPath))
                        {
                            co.BinaryLocation = _browserPath;
                        }
                        List<string> chromeArgs =
                        [
                            "--disable-search-engine-choice-screen",
                            "--disable-features=OptimizationGuideModelDownloading,OptimizationHintsFetching,OptimizationTargetPrediction,OptimizationHints",
                            "--no-sandbox",
                            "--disable-dev-shm-usage"
                        ];

                        co.AddArguments(chromeArgs);

                        webDriver = await Task.Run(() => new ChromeDriver(chromeDriverService, co), cancellationToken);
                        break;

                    case FirefoxDriverService firefoxDriverService:
                        FirefoxOptions fo = new();
                        fo.AddArguments(driverArguments);
                        if (!string.IsNullOrEmpty(_browserPath))
                        {
                            fo.BinaryLocation = _browserPath;
                        }
                        webDriver = await Task.Run(() => new FirefoxDriver(firefoxDriverService, fo), cancellationToken);
                        break;

                    case EdgeDriverService edgeDriverService:
                        EdgeOptions eo = new();
                        eo.AddArguments(driverArguments);
                        if (!string.IsNullOrEmpty(_browserPath))
                        {
                            eo.BinaryLocation = _browserPath;
                        }
                        webDriver = await Task.Run(() => new EdgeDriver(edgeDriverService, eo), cancellationToken);
                        break;

                    case SafariDriverService safariDriverService:
                        SafariOptions so = new();
                        //so.AddArguments(driverArguments); //method doesn't exist for safari
                        if (!string.IsNullOrEmpty(_browserPath))
                        {
                            so.BinaryLocation = _browserPath;
                        }
                        webDriver = await Task.Run(() => new SafariDriver(safariDriverService, so), cancellationToken);
                        break;
                }
            }
            catch (Exception ex) 
            { 
                logger.Error($"Error creating WebDriver for {_browser}", ex);
                return null;
            }

            return webDriver;
        }

        public IMagickImage<byte> GetCroppedScreenshot(IWebDriver driver, IWebElement element)
            => GetCroppedScreenshot(driver, element.Location, element.Size);

        public IMagickImage<byte> GetCroppedScreenshot(IWebDriver driver, System.Drawing.Point location, System.Drawing.Size size)
        {
            Screenshot screenshot = ((ITakesScreenshot)driver).GetScreenshot();
            var imageFactory = new MagickImageFactory();
            using var fullScreenshot = imageFactory.Create(screenshot.AsByteArray);
            fullScreenshot.Resize(_headlessWindowWidth, _headlessWindowHeight); //same size as headless window
            return fullScreenshot.CloneArea(location.X, location.Y, (uint)size.Width, (uint)size.Height);
        }

        private async Task GetPreferredBrowser()
        {
            var browsersInPreferenceOrder = new List<Browser>
            {
                Browser.Chrome,
                Browser.Edge,
                Browser.Firefox,
                Browser.Safari,
            };

            try
            {
                foreach (var browser in browsersInPreferenceOrder)
                {
                    var result = await SeleniumManager.DiscoverBrowserAsync(browser.ToString().ToLower());
                    if (!string.IsNullOrEmpty(result.BrowserPath))
                    {
                        logger.Debug($"Preferred browser found: {browser}");
                        _browserPath = result.BrowserPath;
                        _browser = browser;
                        return;
                    }
                }
                logger.Debug($"No installed browsers found");
                _browser = Browser.None;
            }
            catch (Exception ex)
            {
                logger.Error("Error discovering preferred browser", ex);
                //Set to unknown to it will try again, might have been a temporary issue
                _browser = Browser.Unknown;
            }
        }
    }
}
