using COMPASS.Models;
using COMPASS.Services;
using COMPASS.Tools;
using COMPASS.ViewModels.Import;
using HtmlAgilityPack;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace COMPASS.ViewModels.Sources
{
    public class GmBinderSourceViewModel : SourceViewModel
    {
        public override MetaDataSource Source => MetaDataSource.GmBinder;

        public override bool IsValidSource(Codex codex) =>
            codex.HasOnlineSource() && codex.SourceURL.Contains(new ImportURLViewModel(ImportSource.GmBinder).ExampleURL);

        public override async Task<Codex> GetMetaData(Codex codex)
        {
            // Work on a copy
            codex = new Codex(codex);

            ProgressVM.AddLogEntry(new(LogEntry.MsgType.Info, $"Downloading metadata from GM Binder"));
            Debug.Assert(IsValidSource(codex), "Invalid Codex was used in GM Binder source");
            HtmlDocument doc = await IOService.ScrapeSite(codex.SourceURL);
            HtmlNode src = doc?.DocumentNode;

            if (src is null)
            {
                ProgressVM.AddLogEntry(new(LogEntry.MsgType.Error, $"Could not reach {codex.SourceURL}"));
                return codex;
            }

            //Set known metadata
            codex.Publisher = "GM Binder";

            //get pagecount
            HtmlNode previewDiv = doc.GetElementbyId("preview");
            IEnumerable<HtmlNode> pages = previewDiv.ChildNodes.Where(node => node.Id.Contains('p'));
            codex.PageCount = pages.Count();

            return codex;
        }

        public override async Task<bool> FetchCover(Codex codex)
        {
            if (String.IsNullOrEmpty(codex.SourceURL)) { return false; }
            ProgressVM.AddLogEntry(new(LogEntry.MsgType.Info, $"Downloading cover from {codex.SourceURL}"));
            OpenQA.Selenium.WebDriver driver = await WebDriverService.GetWebDriver();
            try
            {
                await Task.Run(() => driver.Navigate().GoToUrl(codex.SourceURL));
                var coverPage = driver.FindElement(OpenQA.Selenium.By.Id("p1"));
                //screenshot and download the image
                MagickImage image = CoverService.GetCroppedScreenShot(driver, coverPage);
                CoverService.SaveCover(image, codex);
                return true;
            }
            catch (Exception ex)
            {
                string msg = $"Failed to get cover from {codex.SourceURL}";
                Logger.Error(msg, ex);
                ProgressVM.AddLogEntry(new(LogEntry.MsgType.Error, msg));
                return false;
            }
            finally
            {
                driver.Quit();
            }
        }

    }
}
