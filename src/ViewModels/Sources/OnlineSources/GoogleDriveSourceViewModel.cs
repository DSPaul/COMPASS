using COMPASS.Models;
using COMPASS.Tools;
using HtmlAgilityPack;
using System;
using System.Threading.Tasks;

namespace COMPASS.ViewModels.Sources
{
    public class GoogleDriveSourceViewModel : OnlineSourceViewModel
    {
        public override string ImportTitle => "Google Drive";

        public override string ExampleURL => "https://drive.google.com/file/";

        public override ImportSource Source => ImportSource.GoogleDrive;

        public override async Task<Codex> SetMetaData(Codex codex)
        {
            ProgressChanged(new(LogEntry.MsgType.Info, $"Connecting to {ImportTitle}"));

            HtmlDocument doc = await ScrapeSite(InputURL);
            HtmlNode src = doc?.DocumentNode;

            if (src is null)
            {
                ProgressChanged(new(LogEntry.MsgType.Info, "File not found"));
                return codex;
            }

            ProgressChanged(new(LogEntry.MsgType.Info, "Fetching Metadata"));

            //Scrape metadata
            codex = SetWebScrapeHeaderMetadata(codex, src);

            //Set known metadata
            codex.Publisher = "Google Drive";

            MainViewModel.CollectionVM.FilterVM.PopulateMetaDataCollections();
            MainViewModel.CollectionVM.FilterVM.ReFilter();

            return codex;
        }

        public override async Task<bool> FetchCover(Codex codex)
        {
            try
            {
                //cover art is on store page, redirect there by going to /credits which every book has
                HtmlDocument doc = await ScrapeSite(codex.SourceURL);
                HtmlNode src = doc?.DocumentNode;
                if (src is null) return false;

                string imgURL = src.SelectSingleNode("//meta[@property='og:image']").GetAttributeValue("content", String.Empty);
                //cut of "=W***-h***-p" from URL that crops the image if it is present
                if (imgURL.Contains('=')) imgURL = imgURL.Split('=')[0];

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
