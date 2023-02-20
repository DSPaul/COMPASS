using COMPASS.Models;
using COMPASS.Tools;
using HtmlAgilityPack;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.Linq;

namespace COMPASS.ViewModels.Sources
{
    public class GmBinderSourceViewModel : OnlineSourceViewModel
    {
        public override string ImportTitle => "GM Binder";

        public override string ExampleURL => "https://www.gmbinder.com/share/";

        public override ImportSource Source => ImportSource.GmBinder;

        public override Codex SetMetaData(Codex codex)
        {
            HtmlDocument doc = ScrapeSite(InputURL);
            HtmlNode src = doc?.DocumentNode;

            if (src is null)
            {
                return codex;
            }

            worker.ReportProgress(ProgressCounter, new LogEntry(LogEntry.MsgType.Info, "Fetching Metadata"));

            //Scrape metadata
            codex = SetWebScrapeHeaderMetadata(codex, src);

            //Set known metadata
            codex.Publisher = "GM Binder";

            //get pagecount
            HtmlNode previewDiv = doc.GetElementbyId("preview");
            IEnumerable<HtmlNode> pages = previewDiv.ChildNodes.Where(node => node.Id.Contains('p'));
            codex.PageCount = pages.Count();

            return codex;
        }

        public override bool FetchCover(Codex codex)
        {
            OpenQA.Selenium.WebDriver driver = WebDriverFactory.GetWebDriver();
            try
            {
                driver.Navigate().GoToUrl(codex.SourceURL);
                var Coverpage = driver.FindElement(OpenQA.Selenium.By.Id("p1"));
                //screenshot and download the image
                MagickImage image = CoverFetcher.GetCroppedScreenShot(driver, Coverpage);
                CoverFetcher.SaveCover(image, codex);
                return true;
            }
            catch (Exception ex)
            {
                Logger.log.Error(ex.InnerException);
                return false;
            }
            finally
            {
                driver.Quit();
            }
        }
    }
}
