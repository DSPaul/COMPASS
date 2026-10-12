using ImageMagick;
using OpenQA.Selenium;
namespace COMPASS.Infra.Web
{
    public interface IWebDriverService
    {
        Task<WebDriver?> GetWebDriver(CancellationToken cancellationToken = default);

        IMagickImage<byte> GetCroppedScreenshot(IWebDriver driver, IWebElement element);

        IMagickImage<byte> GetCroppedScreenshot(IWebDriver driver, System.Drawing.Point location, System.Drawing.Size size);
    }
}
