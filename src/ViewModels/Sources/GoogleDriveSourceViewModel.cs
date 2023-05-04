using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.ViewModels.Import;
using HtmlAgilityPack;
using System;
using System.Threading.Tasks;

namespace COMPASS.ViewModels.Sources
{
    public class GoogleDriveSourceViewModel : SourceViewModel
    {
        public GoogleDriveSourceViewModel() : base() { }

        public override MetaDataSource Source => MetaDataSource.GoogleDrive;

        public override async Task<Codex> SetMetaData(Codex codex)
        {
            // Work on a copy
            codex = new Codex(codex);

            ProgressVM.AddLogEntry(new(LogEntry.MsgType.Info, $"Connecting to Google Drive"));

            HtmlDocument doc = await Utils.ScrapeSite(codex.SourceURL);
            HtmlNode src = doc?.DocumentNode;

            if (src is null)
            {
                ProgressVM.AddLogEntry(new(LogEntry.MsgType.Info, "File not found"));
                return codex;
            }

            ProgressVM.AddLogEntry(new(LogEntry.MsgType.Info, "Fetching Metadata"));

            //Set known metadata
            codex.Publisher = "Google Drive";

            MainViewModel.CollectionVM.FilterVM.PopulateMetaDataCollections();
            MainViewModel.CollectionVM.FilterVM.ReFilter();

            return codex;
        }

        public override async Task<bool> FetchCover(Codex codex)
        {
            if (String.IsNullOrEmpty(codex.SourceURL)) { return false; }
            try
            {
                //cover art is on store page, redirect there by going to /credits which every book has
                HtmlDocument doc = await Utils.ScrapeSite(codex.SourceURL);
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
                Logger.Error($"Failed to get cover from Google Drive", ex);
                return false;
            }
        }

        public override Codex SetTags(Codex codex) => throw new NotImplementedException();
        public override bool IsValidSource(Codex codex) =>
            codex.HasOnlineSource() && codex.SourceURL.Contains(new ImportURLViewModel(ImportSource.GoogleDrive).ExampleURL);
    }
}
