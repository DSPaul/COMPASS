using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.XmlDtos;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;
using HtmlAgilityPack;

namespace COMPASS.Common.ViewModels.Sources
{
    public class GenericOnlineSourceViewModel : SourceViewModel
    {
        public override MetaDataSource Source => MetaDataSource.GenericURL;

        public override Task<bool> FetchCover(Codex codex) => throw new NotImplementedException();
        public override bool IsValidSource(SourceSet sources) => sources.HasOnlineSource();

        public override async Task<CodexDto> GetMetaData(SourceSet sources)
        {
            ProgressVM.AddLogEntry(new(Severity.Info, $"Extracting metadata from website header"));

            // Scrape metadata
            Debug.Assert(IsValidSource(sources), "Codex without URL was used in Generic URL source");
            HtmlDocument? doc = await IOService.ScrapeSite(sources.SourceURL);
            HtmlNode? src = doc?.DocumentNode;

            if (src is null)
            {
                ProgressVM.AddLogEntry(new(Severity.Error, $"Could not reach {sources.SourceURL}"));
                return new();
            }

            // Use a codex dto to transfer the data
            CodexDto codex = new();

            // Title 
            codex.Title = WebUtility.HtmlDecode(src.SelectSingleNode("//meta[@property='og:title']")?.GetAttributeValue("content", string.Empty) ?? codex.Title);

            // Authors
            string? author = WebUtility.HtmlDecode(src.SelectSingleNode("//meta[@property='og:author']")?.GetAttributeValue("content", string.Empty));
            if (!string.IsNullOrEmpty(author))
            {
                codex.Authors = [author];
            }

            // Description
            codex.Description = WebUtility.HtmlDecode(src.SelectSingleNode("//meta[@property='og:description']")?.GetAttributeValue("content", string.Empty) ?? codex.Description);

            // Tags
            foreach (Tag tag in TargetCollection.AllTags)
            {
                var globs = tag.LinkedGlobs.Concat(tag.CalculatedLinkedGlobs).ToList();
                if (IOService.MatchesAnyGlob(sources.SourceURL, globs))
                {
                    codex.TagIDs.AddIfMissing(tag.ID);
                }
            }
            return codex;
        }
    }
}
