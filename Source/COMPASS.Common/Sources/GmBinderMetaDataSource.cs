using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.ViewModels.Modals.Import;
using HtmlAgilityPack;
using ImageMagick;
using OpenQA.Selenium;

namespace COMPASS.Common.Sources
{
    public class GmBinderMetaDataSource : MetaDataSource
    {
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
            HtmlDocument? doc = await IOService.ScrapeSite(sources.SourceURL);
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

        public override async Task<IMagickImage?> FetchCover(SourceSet sources)
        {
            if (string.IsNullOrEmpty(sources.SourceURL)) { return null; }
            ProgressVM.AddLogEntry(new(Severity.Info, $"Downloading cover from {sources.SourceURL}"));
            WebDriver? driver = await ServiceResolver.Resolve<IWebDriverService>().GetWebDriver().ConfigureAwait(false);

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
            finally
            {
                driver.Quit();
            }
            return null;
        }

    }
}
