using System.Diagnostics;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services;
using COMPASS.Infra.Models.Enums;
using COMPASS.Infra.Tools.Logging;
using HtmlAgilityPack;
using ImageMagick;
using OpenQA.Selenium;

namespace COMPASS.Common.Sources
{
    public class GmBinderMetaDataSource : OnlineMetaDataSource
    {

        public GmBinderMetaDataSource(ILogger logger, IPreferencesService preferencesService, IWebService webService, IWebDriverService webDriverService) :
            base(logger, preferencesService, webService, webDriverService)
        { }
        
        public override MetaDataSourceType Type => MetaDataSourceType.GmBinder;
        public override string UrlPrefix => "https://www.gmbinder.com/share/";

        public override async Task<SourceMetaData> GetMetaData(SourceSet sources, IList<Tag> availableTags)
        {
            Debug.Assert(IsValidSource(sources), "Invalid Codex was used in GM Binder source");
            
            SourceMetaData metaData = new()
            {
                Publisher = "GM Binder"
            };
            
            ProgressVM.AddLogEntry(new(Severity.Info, $"Downloading metadata from GM Binder"));
            HtmlDocument? doc = await WebService.ScrapeSite(sources.SourceURL);
            HtmlNode? src = doc?.DocumentNode;

            if (doc is null || src is null)
            {
                ProgressVM.AddLogEntry(new(Severity.Error, $"Could not reach {sources.SourceURL}"));
                return new();
            }
            

            //get page count
            HtmlNode? previewDiv = doc.GetElementbyId("preview");
            IEnumerable<HtmlNode> pages = previewDiv?.ChildNodes.Where(node => node.Id != null && node.Id.Contains('p')) ?? [];
            metaData.PageCount = pages.Count();

            return metaData;
        }

        public override async Task<IMagickImage<byte>?> FetchCover(SourceSet sources)
        {
            if (string.IsNullOrEmpty(sources.SourceURL)) { return null; }
            ProgressVM.AddLogEntry(new(Severity.Info, $"Downloading cover from {sources.SourceURL}"));
            using WebDriver? driver = await GetWebDriverAsync().ConfigureAwait(false);

            if (driver is null) { return null; }

            try
            {
                await Task.Run(() => driver.Navigate().GoToUrl(sources.SourceURL)).ConfigureAwait(false);
                IWebElement coverPage = driver.FindElement(By.Id("p1"));
                //screenshot and download the image
                return CoverService.GetCroppedScreenShot(driver, coverPage);
            }
            catch (Exception ex)
            {
                string msg = $"Failed to get cover from {sources.SourceURL}";
                Logger.Error(msg, ex);
                ProgressVM.AddLogEntry(new(Severity.Error, msg));
            }
            return null;
        }

    }
}
