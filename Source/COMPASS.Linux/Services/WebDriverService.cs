using COMPASS.Common.Services;

namespace COMPASS.Linux.Services
{
    class WebDriverService : WebDriverServiceBase
    {
        protected override Browser DetectInstalledBrowser() =>
            //TODO: find installs just like on windows, fall back on firefox for now because it comes preinstalled on most distros
            Browser.Firefox;
    }
}
