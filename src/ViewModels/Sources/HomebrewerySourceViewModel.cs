using COMPASS.Models;
using COMPASS.Services;
using COMPASS.Tools;
using COMPASS.ViewModels.Import;
using HtmlAgilityPack;
using ImageMagick;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;

namespace COMPASS.ViewModels.Sources
{
    public class HomebrewerySourceViewModel : SourceViewModel
    {
        public override MetaDataSource Source => MetaDataSource.Homebrewery;
        public override bool IsValidSource(Codex codex)
            => codex.HasOnlineSource() && codex.SourceURL.Contains(new ImportURLViewModel(ImportSource.Homebrewery).ExampleURL);

        public override async Task<Codex> GetMetaData(Codex codex)
        {
            // Work on a copy
            codex = new Codex(codex);

            ProgressVM.AddLogEntry(new(LogEntry.MsgType.Info, $"Downloading metadata from Homebrewery"));
            Debug.Assert(IsValidSource(codex), "Invalid Codex was used in Homebrewery source");
            HtmlDocument? doc = await IOService.ScrapeSite(codex.SourceURL);
            HtmlNode? src = doc?.DocumentNode;

            if (src is null)
            {
                ProgressVM.AddLogEntry(new(LogEntry.MsgType.Error, $"Could not reach {codex.SourceURL}"));
                return codex;
            }

            //Set known metadata
            codex.Publisher = "Homebrewery";

            //Scrape metadata
            //Select script tag with all metadata in JSON format
            string? script = src.SelectSingleNode("/html/body/script[2]")?.InnerText;
            if (script is null || script.Length < 13)
            {
                ProgressVM.AddLogEntry(new(LogEntry.MsgType.Error, $"Failed to read metadata from HomeBrewery"));
                return codex;
            }
            //json is encapsulated by "start_app() function, so cut that out
            string rawData = script[10..^1];
            JObject metadata = JObject.Parse(rawData);

            codex.Title = metadata.SelectToken("brew.title")?.ToString() ?? String.Empty;
            if (metadata.SelectToken("brew.authors")?.Values<string>() is IEnumerable<string> authors) { codex.Authors = new(authors!); }
            codex.Version = metadata.SelectToken("brew.version")?.ToString() ?? String.Empty;
            codex.PageCount = int.Parse(metadata.SelectToken("brew.pageCount")?.ToString() ?? "0");
            codex.Description = metadata.SelectToken("brew.description")?.ToString() ?? String.Empty;
            codex.ReleaseDate = DateTime.Parse(metadata.SelectToken("brew.createdAt")?.ToString().Split('T')[0] ?? String.Empty, CultureInfo.InvariantCulture);

            return codex;
        }

        public override async Task<bool> FetchCover(Codex codex)
        {
            if (String.IsNullOrEmpty(codex.SourceURL)) { return false; }
            ProgressVM.AddLogEntry(new(LogEntry.MsgType.Info, $"Downloading cover from Homebrewery"));
            OpenQA.Selenium.WebDriver driver = await WebDriverService.GetWebDriver();
            try
            {
                string url = codex.SourceURL.Replace("/share/", "/print/"); //use print API to only show doc itself
                await Task.Run(() => driver.Navigate().GoToUrl(url));
                var coverPage = driver.FindElement(OpenQA.Selenium.By.Id("p1"));
                if (coverPage is null)
                {
                    throw new Exception($"Couldn't find p1 on {url}");
                }
                //screenshot and download the image
                IMagickImage image = CoverService.GetCroppedScreenShot(driver, coverPage);
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
