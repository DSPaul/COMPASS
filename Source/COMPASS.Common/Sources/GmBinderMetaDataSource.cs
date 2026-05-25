using System.Diagnostics;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.ViewModels.Modals.Import;
using COMPASS.Infra.Models.Enums;
using COMPASS.Infra.Tools;
using HtmlAgilityPack;
using ImageMagick;
using OpenQA.Selenium;

namespace COMPASS.Common.Sources
{
    public class GmBinderMetaDataSource : MetaDataSource
    {
        private readonly IWebService _webService;
        
        public GmBinderMetaDataSource(CodexCollection targetCollection) :
            base(targetCollection)
        {
            _webService = ServiceResolver.Resolve<IWebService>();
        }
        
        public override MetaDataSourceType Type => MetaDataSourceType.GmBinder;

        public override bool IsValidSource(SourceSet sources) =>
            sources.HasOnlineSource() && sources.SourceURL.Contains(new ImportURLViewModel(ImportSource.GmBinder).ExampleURL);

        public override async Task<SourceMetaData> GetMetaData(SourceSet sources)
        {
            Debug.Assert(IsValidSource(sources), "Invalid Codex was used in GM Binder source");
            
            SourceMetaData metaData = new()
            {
                Publisher = "GM Binder"
            };
            
            ProgressVM.AddLogEntry(new(Severity.Info, $"Downloading metadata from GM Binder"));
            HtmlDocument? doc = await _webService.ScrapeSite(sources.SourceURL);
            HtmlNode? src = doc?.DocumentNode;

            if (doc is null || src is null)
            {
                ProgressVM.AddLogEntry(new(Severity.Error, $"Could not reach {sources.SourceURL}"));
                return new();
            }
            

            //get page count
            HtmlNode? previewDiv = doc.GetElementbyId("preview");
            IEnumerable<HtmlNode> pages = previewDiv?.ChildNodes.Where(node => node.Id.Contains('p')) ?? [];
            metaData.PageCount = pages.Count();

            return metaData;
        }

        public override async Task<IMagickImage<byte>?> FetchCover(SourceSet sources)
        {
            if (string.IsNullOrEmpty(sources.SourceURL)) { return null; }
            ProgressVM.AddLogEntry(new(Severity.Info, $"Downloading cover from {sources.SourceURL}"));
            using WebDriver? driver = await ServiceResolver.Resolve<IWebDriverService>().GetWebDriver().ConfigureAwait(false);

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
