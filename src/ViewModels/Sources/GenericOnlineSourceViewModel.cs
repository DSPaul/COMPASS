using COMPASS.Models;
using COMPASS.Models.Enums;
using COMPASS.Models.XmlDtos;
using COMPASS.Services;
using COMPASS.Tools;
using HtmlAgilityPack;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace COMPASS.ViewModels.Sources
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

            // Use a codex dto to tranfer the data
            CodexDto codex = new();

            // Title 
            codex.Title = WebUtility.HtmlDecode(src.SelectSingleNode("//meta[@property='og:title']")?.GetAttributeValue("content", null) ?? codex.Title);

            // Authors
            string? author = WebUtility.HtmlDecode(src.SelectSingleNode("//meta[@property='og:author']")?.GetAttributeValue("content", String.Empty));
            if (!String.IsNullOrEmpty(author))
            {
                codex.Authors = new() { author };
            }

            // Description
            codex.Description = WebUtility.HtmlDecode(src.SelectSingleNode("//meta[@property='og:description']")?.GetAttributeValue("content", null) ?? codex.Description);

            // Tags
            foreach (var folderTagPair in TargetCollection.Info.FolderTagPairs)
            {
                if (codex.SourceURL.Contains(folderTagPair.Folder))
                {
                    codex.TagIDs.AddIfMissing(folderTagPair.Tag!.ID);
                }
            }
            return codex;
        }
    }
}
