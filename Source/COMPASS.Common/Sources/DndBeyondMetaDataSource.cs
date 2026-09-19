using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Infra.Tools.Logging;
using HtmlAgilityPack;
using ImageMagick;

namespace COMPASS.Common.Sources
{
    public class DndBeyondMetadataSource : OnlineMetadataSource
    {
        public DndBeyondMetadataSource(ILogger logger, IPreferencesService preferencesService, IWebService webService, IWebDriverService webDriverService) :
            base(logger, preferencesService, webService, webDriverService)
        { }
        
        public override MetadataSourceType Type => MetadataSourceType.DnDBeyond;
        public override string UrlPrefix => "https://www.dndbeyond.com/";

        public override async Task<SourceMetadata> GetMetadata(SourceSet sources, IList<Tag> availableTags, CancellationToken cancellationToken = default)
        {
            SourceMetadata metaData = new()
            {
                Publisher = "D&D Beyond",
                Authors = ["Wizards of the Coast"]
            };
            
            //Scrape metadata by going to store page, get to store page by using that /credits redirects there
            Logger.Info($"Connecting to DnD Beyond");
            HtmlDocument? doc = await WebService.ScrapeSite(String.Concat(sources.SourceURL, "/credits"), cancellationToken);
            HtmlNode? src = doc?.DocumentNode;

            return metaData;
        }

        public override async Task<IMagickImage<byte>?> FetchCover(SourceSet sources, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(sources.SourceURL)) { return null; }
            try
            {
                //cover art is on store page, redirect there by going to /credits which every book has
                HtmlDocument? doc = await WebService.ScrapeSite(String.Concat(sources.SourceURL, "/credits"), cancellationToken);
                HtmlNode? src = doc?.DocumentNode;
                if (src is null) return null;

                string? imgURL = src.SelectSingleNode("//img[@class='product-hero-avatar__image']")?.GetAttributeValue("content", string.Empty);

                //download the file
                if (!string.IsNullOrEmpty(imgURL))
                {
                    return await WebService.DownloadImageAsync(imgURL, cancellationToken: cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                //To be handled by the caller
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get cover from DnDBeyond", ex);
            }
            
            return null;
        }

        public override bool IsValidSource(SourceSet sources) => false;
    }
}
