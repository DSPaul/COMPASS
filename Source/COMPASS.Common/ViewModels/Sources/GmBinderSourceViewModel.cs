using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.XmlDtos;
using COMPASS.Common.Services;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.ViewModels.Modals.Import;
using HtmlAgilityPack;
using ImageMagick;

namespace COMPASS.Common.ViewModels.Sources
{
    public class GmBinderSourceViewModel : SourceViewModel
    {
        public override MetaDataSource Source => MetaDataSource.GmBinder;

        public override bool IsValidSource(SourceSet sources) =>
            sources.HasOnlineSource() && sources.SourceURL.Contains(new ImportURLViewModel(ImportSource.GmBinder).ExampleURL);

        public override async Task<CodexDto> GetMetaData(SourceSet sources)
        {
            ProgressVM.AddLogEntry(new(Severity.Info, $"Downloading metadata from GM Binder"));
            Debug.Assert(IsValidSource(sources), "Invalid Codex was used in GM Binder source");
            HtmlDocument? doc = await IOService.ScrapeSite(sources.SourceURL);
            HtmlNode? src = doc?.DocumentNode;

            if (doc is null || src is null)
            {
                ProgressVM.AddLogEntry(new(Severity.Error, $"Could not reach {sources.SourceURL}"));
                return new();
            }

            // Use a codex dto to transfer the data
            CodexDto codex = new()
            {
                Publisher = "GM Binder"
            };

            //get page count
            HtmlNode? previewDiv = doc.GetElementbyId("preview");
            IEnumerable<HtmlNode> pages = previewDiv?.ChildNodes.Where(node => node.Id.Contains('p')) ?? [];
            codex.PageCount = pages.Count();

            return codex;
        }

        public override async Task<bool> FetchCover(Codex codex)
        {
            if (String.IsNullOrEmpty(codex.Sources.SourceURL)) { return false; }
            ProgressVM.AddLogEntry(new(Severity.Info, $"Downloading cover from {codex.Sources.SourceURL}"));
            OpenQA.Selenium.WebDriver? driver = await ServiceResolver.Resolve<IWebDriverService>().GetWebDriver().ConfigureAwait(false);

            if (driver is null) { return false; }

            try
            {
                await Task.Run(() => driver.Navigate().GoToUrl(codex.Sources.SourceURL)).ConfigureAwait(false);
                var coverPage = driver.FindElement(OpenQA.Selenium.By.Id("p1"));
                //screenshot and download the image
                using (IMagickImage image = CoverService.GetCroppedScreenShot(driver, coverPage))
                {
                    await CoverService.SaveCover(codex, image);
                }
                codex.RefreshThumbnail();
                return true;
            }
            catch (Exception ex)
            {
                string msg = $"Failed to get cover from {codex.Sources.SourceURL}";
                Logger.Error(msg, ex);
                ProgressVM.AddLogEntry(new(Severity.Error, msg));
                return false;
            }
            finally
            {
                driver.Quit();
            }
        }

    }
}
