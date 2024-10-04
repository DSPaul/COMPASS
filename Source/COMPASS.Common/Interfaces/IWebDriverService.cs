using OpenQA.Selenium;
using System.Threading.Tasks;

namespace COMPASS.Common.Interfaces
{
    public interface IWebDriverService
    {
        Task<WebDriver?> GetWebDriver();
    }
}
