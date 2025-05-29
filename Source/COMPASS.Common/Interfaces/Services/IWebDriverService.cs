using System.Threading.Tasks;
using OpenQA.Selenium;

namespace COMPASS.Common.Interfaces.Services
{
    public interface IWebDriverService
    {
        Task<WebDriver?> GetWebDriver();
    }
}
