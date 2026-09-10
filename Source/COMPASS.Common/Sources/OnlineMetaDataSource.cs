using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using OpenQA.Selenium;

namespace COMPASS.Common.Sources
{
    /// <summary>
    /// A baseclass for metadatasources that use the SourceURL to get metadata from a website
    /// </summary>
    public abstract class OnlineMetaDataSource(
        ILogger logger,
        IPreferencesService preferencesService,
        IWebService webService,
        IWebDriverService webDriverService) : MetaDataSource(logger, preferencesService)
    {
        protected IWebService WebService => webService;
        protected private async Task<WebDriver?> GetWebDriverAsync() => await webDriverService.GetWebDriver().ConfigureAwait(false);

        /// <summary>
        /// The prefix of the URL for this source. For example, "https://www.example.com/metadata/".
        /// </summary>
        public abstract string UrlPrefix { get; }

        public override bool IsValidSource(SourceSet sources) => sources.HasOnlineSource() && sources.SourceURL.StartsWith(UrlPrefix);
    }
}
