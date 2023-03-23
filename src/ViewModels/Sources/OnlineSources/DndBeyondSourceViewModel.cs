using COMPASS.Models;
using COMPASS.Tools;
using HtmlAgilityPack;
using System;
using System.Threading.Tasks;

namespace COMPASS.ViewModels.Sources
{
    public class DndBeyondSourceViewModel : OnlineSourceViewModel
    {
        public override string ImportTitle => "D&D Beyond";

        public override string ExampleURL => "https://www.dndbeyond.com/sources/";

        public override ImportSource Source => ImportSource.DnDBeyond;

        public override async Task<Codex> SetMetaData(Codex codex)
        {
            //Scrape metadata by going to storepage, get to storepage by using that /credits redirects there
            ProgressChanged(new(LogEntry.MsgType.Info, $"Connecting to {ImportTitle}"));
            HtmlDocument doc = await ScrapeSite(String.Concat(InputURL, "/credits"));
            HtmlNode src = doc?.DocumentNode;

            if (src is null)
            {
                return codex;
            }

            ProgressChanged(new(LogEntry.MsgType.Info, "Fetching Metadata"));

            //Scrape headers
            codex = SetWebScrapeHeaderMetadata(codex, src);

            //Set known metadata
            codex.Publisher = "D&D Beyond";
            codex.Authors = new() { "Wizards of the Coast" };

            MainViewModel.CollectionVM.FilterVM.PopulateMetaDataCollections();
            MainViewModel.CollectionVM.FilterVM.ReFilter();

            return codex;
        }

        public override bool FetchCover(Codex codex)
        {
            try
            {
                //cover art is on store page, redirect there by going to /credits which every book has
                HtmlDocument doc = ScrapeSite(String.Concat(codex.SourceURL, "/credits")).Result;
                HtmlNode src = doc?.DocumentNode;
                if (src is null) return false;

                string imgURL = src.SelectSingleNode("//img[@class='product-hero-avatar__image']").GetAttributeValue("content", String.Empty);

                //download the file
                CoverFetcher.SaveCover(imgURL, codex);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get cover from {ImportTitle}", ex);
                return false;
            }
        }
    }
}
