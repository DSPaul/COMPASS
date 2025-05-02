using System.Threading.Tasks;
using OpenQA.Selenium;

namespace COMPASS.Common.Interfaces
{
    public interface IWebDriverService
    {
        Task<WebDriver?> GetWebDriver();
    }
}
