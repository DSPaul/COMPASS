using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Infra.Interfaces.Services;
using COMPASS.Infra.Models.Enums;
using COMPASS.Infra.Tools.Logging;
using HtmlAgilityPack;
using ImageMagick;

namespace COMPASS.Common.Sources
{
    public class DndBeyondMetaDataSource : OnlineMetaDataSource
    {
        public DndBeyondMetaDataSource(ILogger logger, IPreferencesService preferencesService, IWebService webService, IWebDriverService webDriverService) :
            base(logger, preferencesService, webService, webDriverService)
        { }
        
        public override MetaDataSourceType Type => MetaDataSourceType.DnDBeyond;
        public override string UrlPrefix => "https://www.dndbeyond.com/";

        public override async Task<SourceMetaData> GetMetaData(SourceSet sources, IList<Tag> availableTags)
        {
            SourceMetaData metaData = new()
            {
                Publisher = "D&D Beyond",
                Authors = ["Wizards of the Coast"]
            };
            
            //Scrape metadata by going to store page, get to store page by using that /credits redirects there
            ProgressVM.AddLogEntry(new(Severity.Info, $"Connecting to DnD Beyond"));
            HtmlDocument? doc = await WebService.ScrapeSite(String.Concat(sources.SourceURL, "/credits"));
            HtmlNode? src = doc?.DocumentNode;

            return metaData;
        }

        public override async Task<IMagickImage<byte>?> FetchCover(SourceSet sources)
        {
            if (string.IsNullOrEmpty(sources.SourceURL)) { return null; }
            try
            {
                //cover art is on store page, redirect there by going to /credits which every book has
                HtmlDocument? doc = await WebService.ScrapeSite(String.Concat(sources.SourceURL, "/credits"));
                HtmlNode? src = doc?.DocumentNode;
                if (src is null) return null;

                string? imgURL = src.SelectSingleNode("//img[@class='product-hero-avatar__image']")?.GetAttributeValue("content", string.Empty);

                //download the file
                if (!string.IsNullOrEmpty(imgURL))
                {
                    return await WebService.DownloadImageAsync(imgURL);
                }
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
