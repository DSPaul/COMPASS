using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.ViewModels.Import;
using HtmlAgilityPack;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace COMPASS.ViewModels.Sources
{
    public class GmBinderSourceViewModel : SourceViewModel
    {
        public GmBinderSourceViewModel() : base() { }
        public GmBinderSourceViewModel(CodexCollection targetCollection) : base(targetCollection) { }

        public override MetaDataSource Source => MetaDataSource.GmBinder;

        public override bool IsValidSource(Codex codex) =>
            codex.HasOnlineSource() && codex.SourceURL.Contains(new ImportURLViewModel(ImportSource.GmBinder).ExampleURL);

        public override async Task<Codex> GetMetaData(Codex codex)
        {
            // Work on a copy
            codex = new Codex(codex);

            HtmlDocument doc = await Utils.ScrapeSite(codex.SourceURL);
            HtmlNode src = doc?.DocumentNode;

            if (src is null)
            {
                return codex;
            }

            ProgressVM.AddLogEntry(new(LogEntry.MsgType.Info, "Fetching Metadata"));

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

        public override async Task<bool> FetchCover(Codex codex)
        {
            if (String.IsNullOrEmpty(codex.SourceURL)) { return false; }
            OpenQA.Selenium.WebDriver driver = WebDriverFactory.GetWebDriver();
            try
            {
                await Task.Run(() => driver.Navigate().GoToUrl(codex.SourceURL));
                var Coverpage = driver.FindElement(OpenQA.Selenium.By.Id("p1"));
                //screenshot and download the image
                MagickImage image = CoverFetcher.GetCroppedScreenShot(driver, Coverpage);
                CoverFetcher.SaveCover(image, codex);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get cover from GM Binder", ex);
                return false;
            }
            finally
            {
                driver.Quit();
            }
        }

    }
}
