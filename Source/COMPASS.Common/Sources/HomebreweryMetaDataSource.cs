using System.Diagnostics;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.ViewModels.Modals.Import;
using COMPASS.Infra.Models.Enums;
using COMPASS.Infra.Tools;
using ImageMagick;
using System.Text.Json.Nodes;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace COMPASS.Common.Sources
{
    public class HomebreweryMetaDataSource : MetaDataSource
    {
        private readonly IWebService _webService;

        public HomebreweryMetaDataSource(CodexCollection targetCollection) :
            base(targetCollection)
        {
            _webService = ServiceResolver.Resolve<IWebService>();
        }
        
        public override MetaDataSourceType Type => MetaDataSourceType.Homebrewery;
        public override bool IsValidSource(SourceSet sources)
            => sources.HasOnlineSource() && sources.SourceURL.Contains(new ImportURLViewModel(ImportSource.Homebrewery).ExampleURL);

        public override async Task<SourceMetaData> GetMetaData(SourceSet sources)
        {
            Debug.Assert(IsValidSource(sources), "Invalid Codex was used in Homebrewery source");

            string uri = sources.SourceURL.Replace(@"/share/", @"/metadata/");
            
            SourceMetaData metaData = new()
            {
                Publisher = "Homebrewery",
            };

            ProgressVM.AddLogEntry(new(Severity.Info, $"Downloading metadata from Homebrewery"));
            JsonNode? metadata = await _webService.GetJsonAsync(uri);

            if (metadata is null || metadata.AsObject().Count == 0)
            {
                string message = $"homebrew {sources.SourceURL} was not found on homebrewery \n" +
                    $"Please check the url and check if the homebrewery.naturalcrit.com website is up.";
                ProgressVM.AddLogEntry(new(Severity.Warning, message));
                Logger.Warn($"Could not find homebrew {sources.SourceURL} on homebrewery");
                return new();
            }

            metaData.Title = metadata["title"]?.GetValue<string>() ?? string.Empty;
            var authors = metadata["authors"]?.AsArray().Select(a => a?.GetValue<string>()) ?? [];
            metaData.Authors = authors
                .Where(author => !string.IsNullOrWhiteSpace(author))
                .Cast<string>()
                .ToList();
            metaData.PageCount = int.Parse(metadata["pageCount"]?.GetValue<int>().ToString() ?? "0");
            metaData.Description = metadata["description"]?.GetValue<string>() ?? string.Empty;
            metaData.ReleaseDate = metadata["createdAt"]?.GetValue<DateTime>();

            return metaData;
        }

        public override async Task<IMagickImage<byte>?> FetchCover(SourceSet sources)
        {
            if (string.IsNullOrEmpty(sources.SourceURL)) { return null; }
            ProgressVM.AddLogEntry(new(Severity.Info, $"Downloading cover from Homebrewery"));
            WebDriver? driver = await ServiceResolver.Resolve<IWebDriverService>().GetWebDriver().ConfigureAwait(false);

            if (driver == null) { return null; }

            WebDriverWait wait = new(driver, TimeSpan.FromSeconds(5));
            try
            {
                string url = sources.SourceURL;
                var frameSelector = By.Id("BrewRenderer");
                var pageSelector = By.Id("p1");

                await Task.Run(() =>
                {
                    driver.Navigate().GoToUrl(url);
                    wait.Until(ExpectedConditions.ElementExists(frameSelector));
                }).ConfigureAwait(false);

                wait.Until(ExpectedConditions.ElementExists(frameSelector));

                Thread.Sleep(500);

                IWebElement frame = driver.FindElement(frameSelector);
                System.Drawing.Point location = frame.Location;

                await Task.Run(() =>
                {
                    //TODO add cancelationtoken when redoing backgroup processs system
                    wait.Until(ExpectedConditions.FrameToBeAvailableAndSwitchToIt(frameSelector));
                    wait.Until(ExpectedConditions.ElementExists(pageSelector));
                }).ConfigureAwait(false);

                Thread.Sleep(500);

                IWebElement coverPage = driver.FindElement(pageSelector);
                location.X += coverPage.Location.X;
                location.Y += coverPage.Location.Y;

                //screenshot and download the image
                return CoverService.GetCroppedScreenShot(driver, location, coverPage.Size) as MagickImage;
            }
            catch (Exception ex)
            {
                string msg = $"Failed to get cover from {sources.SourceURL}";
                Logger.Error(msg, ex);
                ProgressVM.AddLogEntry(new(Severity.Error, msg));
            }
            finally
            {
                driver.Quit();
            }

            return null;
        }
    }
}
