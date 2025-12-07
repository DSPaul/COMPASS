using COMPASS.Common.Services;

namespace COMPASS.Tests.Common.Mocks
{
    public class MockWebDriverService : WebDriverServiceBase
    {
        //Use chrome for testing, can be installed on every platform
        protected override Browser DetectInstalledBrowser() => Browser.Chrome;
    }
}
