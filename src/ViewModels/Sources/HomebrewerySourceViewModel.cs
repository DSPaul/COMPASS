using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.ViewModels.Import;
using HtmlAgilityPack;
using ImageMagick;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace COMPASS.ViewModels.Sources
{
    public class HomebrewerySourceViewModel : SourceViewModel
    {
        public HomebrewerySourceViewModel() : base() { }
        public HomebrewerySourceViewModel(CodexCollection targetCollection) : base(targetCollection) { }

        public override MetaDataSource Source => MetaDataSource.Homebrewery;
        public override bool IsValidSource(Codex codex)
            => codex.HasOnlineSource() && codex.SourceURL.Contains(new ImportURLViewModel(ImportSource.Homebrewery).ExampleURL);

        public override async Task<Codex> GetMetaData(Codex codex)
        {
            // Work on a copy
            codex = new Codex(codex);

            ProgressVM.AddLogEntry(new(LogEntry.MsgType.Info, $"Downloading metadata from Homebrewery"));

            HtmlDocument doc = await Utils.ScrapeSite(codex.SourceURL);
            HtmlNode src = doc?.DocumentNode;

            if (src is null)
            {
                ProgressVM.AddLogEntry(new(LogEntry.MsgType.Error, $"Could not reach {codex.SourceURL}"));
                return codex;
            }

            //Set known metadata
            codex.Publisher = "Homebrewery";

            //Scrape metadata
            //Select script tag with all metadata in JSON format
            string script = src.SelectSingleNode("/html/body/script[2]").InnerText;
            //json is encapsulated by "start_app() function, so cut that out
            string rawData = script[10..^1];
            JObject metadata = JObject.Parse(rawData);

            codex.Title = (string)metadata.SelectToken("brew.title");
            var authors = metadata.SelectToken("brew.authors")?.Values<string>();
            if (authors is not null) { codex.Authors = new(authors); }
            codex.Version = (string)metadata.SelectToken("brew.version");
            codex.PageCount = (int)metadata.SelectToken("brew.pageCount");
            codex.Description = (string)metadata.SelectToken("brew.description");
            codex.ReleaseDate = DateTime.Parse(((string)metadata.SelectToken("brew.createdAt"))?.Split('T')[0], CultureInfo.InvariantCulture);

            return codex;
        }

        public override async Task<bool> FetchCover(Codex codex)
        {
            if (String.IsNullOrEmpty(codex.SourceURL)) { return false; }
            ProgressVM.AddLogEntry(new(LogEntry.MsgType.Info, $"Downloading cover from Homebrewery"));
            OpenQA.Selenium.WebDriver driver = WebDriverFactory.GetWebDriver();
            try
            {
                string URL = codex.SourceURL.Replace("/share/", "/print/"); //use print API to only show doc itself
                await Task.Run(() => driver.Navigate().GoToUrl(URL));
                var Coverpage = driver.FindElement(OpenQA.Selenium.By.Id("p1"));
                //screenshot and download the image
                MagickImage image = CoverFetcher.GetCroppedScreenShot(driver, Coverpage);
                CoverFetcher.SaveCover(image, codex);
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
