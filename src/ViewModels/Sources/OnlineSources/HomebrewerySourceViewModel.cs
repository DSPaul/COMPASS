using COMPASS.Models;
using COMPASS.Tools;
using HtmlAgilityPack;
using ImageMagick;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;

namespace COMPASS.ViewModels.Sources
{
    public class HomebrewerySourceViewModel : OnlineSourceViewModel
    {
        public override string ImportTitle => "Homebrewery";

        public override string ExampleURL => "https://homebrewery.naturalcrit.com/share/";

        public override ImportSource Source => ImportSource.Homebrewery;

        public override bool ShowValidateDisableCheckbox => true;

        public override Codex SetMetaData(Codex codex)
        {
            worker.ReportProgress(ProgressCounter, new LogEntry(LogEntry.MsgType.Info, $"Connecting to {ImportTitle}"));

            HtmlDocument doc = ScrapeSite(InputURL);
            HtmlNode src = doc?.DocumentNode;

            if (src is null)
            {
                return codex;
            }

            worker.ReportProgress(ProgressCounter, new LogEntry(LogEntry.MsgType.Info, "Fetching metadata"));

            //Set known metadata
            codex.Publisher = "Homebrewery";

            //Scrape metadata
            //Select script tag with all metadata in JSON format
            string script = src.SelectSingleNode("/html/body/script[2]").InnerText;
            //json is encapsulated by "start_app() function, so cut that out
            string rawData = script[10..^1];
            JObject metadata = JObject.Parse(rawData);

            codex.Title = (string)metadata.SelectToken("brew.title");
            codex.Authors = new(metadata.SelectToken("brew.authors")?.Values<string>());
            codex.Version = (string)metadata.SelectToken("brew.version");
            codex.PageCount = (int)metadata.SelectToken("brew.pageCount");
            codex.Description = (string)metadata.SelectToken("brew.description");
            codex.ReleaseDate = DateTime.Parse(((string)metadata.SelectToken("brew.createdAt"))?.Split('T')[0], CultureInfo.InvariantCulture);

            return codex;
        }

        public override bool FetchCover(Codex codex)
        {
            OpenQA.Selenium.WebDriver driver = WebDriverFactory.GetWebDriver();
            try
            {
                string URL = codex.SourceURL.Replace("/share/", "/print/"); //use print API to only show doc itself
                driver.Navigate().GoToUrl(URL);
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
