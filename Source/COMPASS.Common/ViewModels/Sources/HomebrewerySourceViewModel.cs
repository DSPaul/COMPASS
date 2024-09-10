using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.XmlDtos;
using COMPASS.Common.Services;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Import;
using HtmlAgilityPack;
using ImageMagick;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace COMPASS.Common.ViewModels.Sources
{
    public class HomebrewerySourceViewModel : SourceViewModel
    {
        public override MetaDataSource Source => MetaDataSource.Homebrewery;
        public override bool IsValidSource(SourceSet sources)
            => sources.HasOnlineSource() && sources.SourceURL.Contains(new ImportURLViewModel(ImportSource.Homebrewery).ExampleURL);

        public override async Task<CodexDto> GetMetaData(SourceSet sources)
        {
            ProgressVM.AddLogEntry(new(Severity.Info, $"Downloading metadata from Homebrewery"));
            Debug.Assert(IsValidSource(sources), "Invalid Codex was used in Homebrewery source");
            HtmlDocument? doc = await IOService.ScrapeSite(sources.SourceURL);
            HtmlNode? src = doc?.DocumentNode;

            if (src is null)
            {
                ProgressVM.AddLogEntry(new(Severity.Error, $"Could not reach {sources.SourceURL}"));
                return new();
            }

            // Use a codex dto to tranfer the data
            CodexDto codex = new()
            {
                Publisher = "Homebrewery"
            };

            //Scrape metadata
            //Select script tag with all metadata in JSON format
            string? script = src.SelectSingleNode("/html/body/script[2]")?.InnerText;
            if (script is null || script.Length < 13)
            {
                ProgressVM.AddLogEntry(new(Severity.Error, $"Failed to read metadata from HomeBrewery"));
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
            if (String.IsNullOrEmpty(codex.Sources.SourceURL)) { return false; }
            ProgressVM.AddLogEntry(new(Severity.Info, $"Downloading cover from Homebrewery"));
            WebDriver driver = await WebDriverService.GetWebDriver();
            WebDriverWait wait = new(driver, TimeSpan.FromSeconds(5));
            try
            {
                string url = codex.Sources.SourceURL;
                var frameSelector = By.Id("BrewRenderer");
                var pageSelector = By.Id("p1");

                await Task.Run(() =>
                {
                    driver.Navigate().GoToUrl(url);
                    wait.Until(ExpectedConditions.ElementExists(frameSelector));
                });

                wait.Until(ExpectedConditions.ElementExists(frameSelector));

                Thread.Sleep(500);

                var frame = driver.FindElement(frameSelector);
                Point location = frame.Location;

                await Task.Run(() =>
                {
                    wait.Until(ExpectedConditions.FrameToBeAvailableAndSwitchToIt(frameSelector));
                    wait.Until(ExpectedConditions.ElementExists(pageSelector));
                });

                Thread.Sleep(500);

                var coverPage = driver.FindElement(pageSelector);
                location.X += coverPage.Location.X;
                location.Y += coverPage.Location.Y;

                //screenshot and download the image
                IMagickImage image = CoverService.GetCroppedScreenShot(driver, location, coverPage.Size);
                CoverService.SaveCover(image, codex);
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
