using COMPASS.Models;
using COMPASS.Models.Enums;
using COMPASS.Models.XmlDtos;
using COMPASS.Services;
using COMPASS.Tools;
using COMPASS.ViewModels.Import;
using ImageMagick;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace COMPASS.ViewModels.Sources
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

            string uri = sources.SourceURL.Replace(@"/share/", @"/metadata/");

            JObject? metadata = await IOService.GetJsonAsync(uri);

            if (metadata is null || !metadata.HasValues)
            {
                string message = $"homebrew {sources.SourceURL} was not found on homebrewery \n" +
                    $"Please check the url and check if the homebrewery.naturalcrit.com website is up.";
                ProgressVM.AddLogEntry(new(Severity.Warning, message));
                Logger.Warn($"Could not find homebrew {sources.SourceURL} on homebrewery");
                return new();
            }

            // Use a codex dto to tranfer the data
            CodexDto codex = new()
            {
                Publisher = "Homebrewery"
            };

            codex.Title = metadata.SelectToken("title")?.ToString() ?? String.Empty;
            if (metadata.SelectToken("authors")?.Values<string>() is IEnumerable<string> authors)
            {
                codex.Authors = new(authors!);
            }
            codex.PageCount = int.Parse(metadata.SelectToken("pageCount")?.ToString() ?? "0");
            codex.Description = metadata.SelectToken("description")?.ToString() ?? String.Empty;
            codex.ReleaseDate = metadata.SelectToken("createdAt")?.Value<DateTime>();

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
                using (IMagickImage image = CoverService.GetCroppedScreenShot(driver, location, coverPage.Size))
                {
                    CoverService.SaveCover(codex, image);
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
