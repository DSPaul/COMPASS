using COMPASS.Models;
using COMPASS.Tools;
using HtmlAgilityPack;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace COMPASS.ViewModels.Sources
{
    public class GmBinderSourceViewModel : OnlineSourceViewModel
    {
        public override string ImportTitle => "GM Binder";

        public override string ExampleURL => "https://www.gmbinder.com/share/";

        public override ImportSource Source => ImportSource.GmBinder;

        public override async Task<Codex> SetMetaData(Codex codex)
        {
            HtmlDocument doc = await ScrapeSite(InputURL);
            HtmlNode src = doc?.DocumentNode;

            if (src is null)
            {
                return codex;
            }

            ProgressChanged(new(LogEntry.MsgType.Info, "Fetching Metadata"));

            //Scrape metadata
            codex = SetWebScrapeHeaderMetadata(codex, src);

            //Set known metadata
            codex.Publisher = "GM Binder";

            //get pagecount
            HtmlNode previewDiv = doc.GetElementbyId("preview");
            IEnumerable<HtmlNode> pages = previewDiv.ChildNodes.Where(node => node.Id.Contains('p'));
            codex.PageCount = pages.Count();

            MainViewModel.CollectionVM.FilterVM.PopulateMetaDataCollections();
            MainViewModel.CollectionVM.FilterVM.ReFilter();

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
                Logger.Error($"Failed to get cover from {ImportTitle}", ex);
                return false;
            }
            finally
            {
                driver.Quit();
            }
        }
    }
}
