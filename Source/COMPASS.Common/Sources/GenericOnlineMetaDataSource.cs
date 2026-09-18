using System.Diagnostics;
using System.Net;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Tools.Logging;
using HtmlAgilityPack;
using ImageMagick;

namespace COMPASS.Common.Sources
{
    public class GenericOnlineMetaDataSource : OnlineMetaDataSource
    {
        public GenericOnlineMetaDataSource(ILogger logger, IPreferencesService preferencesService, IWebService webService, IWebDriverService webDriverService) :
            base(logger, preferencesService, webService, webDriverService)
        { }
        
        public override MetaDataSourceType Type => MetaDataSourceType.GenericURL;
        
        public override string UrlPrefix => "https://";
        protected override IEnumerable<string> AcceptedUrlPrefixes => ["https://", "http://"];

        public override Task<IMagickImage<byte>?> FetchCover(SourceSet sources, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public override async Task<SourceMetaData> GetMetaData(SourceSet sources, IList<Tag> availableTags, CancellationToken cancellationToken = default)
        {
            Debug.Assert(IsValidSource(sources), "Codex without URL was used in Generic URL source");
            
            SourceMetaData metaData = new();

            // Scrape metadata
            Logger.Info($"Extracting metadata from website header");
            HtmlDocument? doc = await WebService.ScrapeSite(sources.SourceURL, cancellationToken);
            HtmlNode? src = doc?.DocumentNode;

            if (src is null)
            {
                Logger.Error($"Could not reach {sources.SourceURL}", new HttpRequestException());
                return metaData;
            }

            // Title 
            string? title = src.SelectSingleNode("//meta[@property='og:title']")?.GetAttributeValue("content", string.Empty);
            title = string.IsNullOrEmpty(title)
                ? src.SelectSingleNode("//head/title")?.InnerText
                : title;
            if (!string.IsNullOrEmpty(title))
            {
                metaData.Title = WebUtility.HtmlDecode(title);
            }

            // Authors
            string? author = src.SelectSingleNode("//meta[@property='og:author']")?.GetAttributeValue("content", string.Empty);
            if (!string.IsNullOrEmpty(author))
            {
                metaData.Authors = [WebUtility.HtmlDecode(author)];
            }

            // Description
            string? description = src.SelectSingleNode("//meta[@property='og:description']")?.GetAttributeValue("content", string.Empty);
            if (!string.IsNullOrEmpty(description))
            {
                metaData.Description = WebUtility.HtmlDecode(description);
            }

            // Tags
            foreach (var tag in GetMatchingTags(sources, availableTags))
            {
                metaData.Tags.AddIfMissing(tag);
            }

            return metaData;
        }
    }
}
