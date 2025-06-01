using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;
using HtmlAgilityPack;
using ImageMagick;

namespace COMPASS.Common.Sources
{
    public class GenericOnlineMetaDataSource : MetaDataSource
    {
        public override MetaDataSourceType Type => MetaDataSourceType.GenericURL;

        public override Task<IMagickImage?> FetchCover(SourceSet sources) => throw new NotImplementedException();
        public override bool IsValidSource(SourceSet sources) => sources.HasOnlineSource();

        public override async Task<SourceMetaData> GetMetaData(SourceSet sources)
        {
            Debug.Assert(IsValidSource(sources), "Codex without URL was used in Generic URL source");
            
            SourceMetaData metaData = new();

            // Scrape metadata
            ProgressVM.AddLogEntry(new(Severity.Info, $"Extracting metadata from website header"));
            HtmlDocument? doc = await IOService.ScrapeSite(sources.SourceURL);
            HtmlNode? src = doc?.DocumentNode;

            if (src is null)
            {
                ProgressVM.AddLogEntry(new(Severity.Error, $"Could not reach {sources.SourceURL}"));
                return metaData;
            }

            // Title 
            string? title = src.SelectSingleNode("//meta[@property='og:title']")?.GetAttributeValue("content", string.Empty);
            if(!string.IsNullOrEmpty(title))
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
            foreach (Tag tag in TargetCollection.AllTags)
            {
                var globs = tag.LinkedGlobs.Concat(tag.CalculatedLinkedGlobs).ToList();
                if (IOService.MatchesAnyGlob(sources.SourceURL, globs))
                {
                    metaData.Tags.AddIfMissing(tag);
                }
            }
            return metaData;
        }
    }
}
