using OpenQA.Selenium;
namespace COMPASS.Infra.Web
{
    public interface IWebDriverService
    {
        Task<WebDriver?> GetWebDriver(CancellationToken cancellationToken = default);
    }
}
