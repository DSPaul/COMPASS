using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Tools.Logging;
using ImageMagick;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Diagnostics;
using System.Text.Json.Nodes;

namespace COMPASS.Common.Sources
{
    public class HomebreweryMetadataSource : OnlineMetadataSource
    {
        public HomebreweryMetadataSource(ILogger logger, IPreferencesService preferencesService, IWebService webService, IWebDriverService webDriverService) :
            base(logger, preferencesService, webService, webDriverService)
        { }
        
        public override MetadataSourceType Type => MetadataSourceType.Homebrewery;
        public override string UrlPrefix => "https://homebrewery.naturalcrit.com/share/";

        public override async Task<SourceMetadata> GetMetadata(SourceSet sources, IList<Tag> availableTags, CancellationToken cancellationToken = default)
        {
            Debug.Assert(IsValidSource(sources), "Invalid Codex was used in Homebrewery source");

            string uri = sources.SourceURL.Replace(@"/share/", @"/metadata/");
            
            SourceMetadata metaData = new()
            {
                Publisher = "Homebrewery",
            };

            Logger.Info($"Downloading metadata from Homebrewery");
            JsonNode? metadata = await WebService.GetJsonAsync(uri, cancellationToken);

            if (metadata is null || metadata.AsObject().Count == 0)
            {
                string message = $"homebrew {sources.SourceURL} was not found on homebrewery \n" +
                    $"Please check the url and check if the homebrewery.naturalcrit.com website is up.";
                Logger.Warn(message);
                Logger.Warn($"Could not find homebrew {sources.SourceURL} on homebrewery");
                return new();
            }

            metaData.Title = metadata["title"]?.GetValue<string>() ?? string.Empty;
            var authors = metadata["authors"]?.AsArray().Select(a => a?.GetValue<string>()) ?? [];
            metaData.Authors = authors
                .Where(author => !string.IsNullOrWhiteSpace(author))
                .Cast<string>()
                .ToList();
            metaData.PageCount = int.Parse(metadata["pageCount"]?.GetIntValue().ToString() ?? "0");
            metaData.Description = metadata["description"]?.GetValue<string>() ?? string.Empty;
            metaData.ReleaseDate = metadata["createdAt"]?.GetValue<DateTime>();

            return metaData;
        }

        public override async Task<IMagickImage<byte>?> FetchCover(SourceSet sources, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(sources.SourceURL)) { return null; }
            Logger.Info($"Downloading cover from Homebrewery");
            using WebDriver? driver = await GetWebDriverAsync(cancellationToken).ConfigureAwait(false);

            if (driver == null) { return null; }

            WebDriverWait wait = new(driver, TimeSpan.FromSeconds(5));
            wait.IgnoreExceptionTypes([typeof(NoSuchElementException)]);

            try
            {
                string url = sources.SourceURL;
                var frameSelector = By.Id("BrewRenderer");
                var pageSelector = By.Id("p1");

                await driver.Navigate().GoToUrlAsync(url).ConfigureAwait(false);
                wait.Until(d => d.FindElement(frameSelector), cancellationToken);

                IWebElement frame = driver.FindElement(frameSelector);
                System.Drawing.Point location = frame.Location;

                //TODO add cancelationtoken when redoing background processs system
                wait.Until(d => d.SwitchTo().Frame(frame), cancellationToken);
                wait.Until(d => d.FindElement(pageSelector)?.Displayed == true, cancellationToken);

                await Task.Delay(1000, cancellationToken); //Homebrewery can take some time to render everything

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
            }

            return null;
        }
    }
}
