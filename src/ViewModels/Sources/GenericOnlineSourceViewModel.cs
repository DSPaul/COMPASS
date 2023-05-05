using COMPASS.Models;
using COMPASS.Tools;
using HtmlAgilityPack;
using System;
using System.Threading.Tasks;

namespace COMPASS.ViewModels.Sources
{
    public class GenericOnlineSourceViewModel : SourceViewModel
    {
        public GenericOnlineSourceViewModel() : base() { }

        public override MetaDataSource Source => MetaDataSource.GenericURL;

        public override Task<bool> FetchCover(Codex codex) => throw new NotImplementedException();
        public override bool IsValidSource(Codex codex) => codex.HasOnlineSource();

        public override async Task<Codex> GetMetaData(Codex codex)
        {
            // Work on a copy
            codex = new Codex(codex);

            ProgressVM.AddLogEntry(new(LogEntry.MsgType.Info, $"Connecting to {codex.SourceURL}"));

            // Scrape metadata
            HtmlDocument doc = await Utils.ScrapeSite(codex.SourceURL);
            HtmlNode src = doc?.DocumentNode;

            if (src is null)
            {
                return codex;
            }

            ProgressVM.AddLogEntry(new(LogEntry.MsgType.Info, "Fetching Metadata"));

            // Title 
            codex.Title = src.SelectSingleNode("//meta[@property='og:title']")?.GetAttributeValue("content", null) ?? codex.Title;

            // Authors
            string author = src.SelectSingleNode("//meta[@property='og:author']")?.GetAttributeValue("content", String.Empty);
            if (!String.IsNullOrEmpty(author))
            {
                codex.Authors = new() { author };
            }

            // Description
            codex.Description = src.SelectSingleNode("//meta[@property='og:description']")?.GetAttributeValue("content", null) ?? codex.Description;

            // Tags
            foreach (var folderTagPair in TargetCollection.Info.FolderTagPairs)
            {
                if (codex.SourceURL.Contains(folderTagPair.Folder))
                {
                    codex.Tags.AddIfMissing(folderTagPair.Tag);
                }
            }

            MainViewModel.CollectionVM.FilterVM.PopulateMetaDataCollections();
            MainViewModel.CollectionVM.FilterVM.ReFilter();

            return codex;
        }
    }
}
