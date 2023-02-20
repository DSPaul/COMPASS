using COMPASS.Models;
using COMPASS.Tools;
using HtmlAgilityPack;
using System;

namespace COMPASS.ViewModels.Sources
{
    public class GoogleDriveSourceViewModel : OnlineSourceViewModel
    {
        public override string ImportTitle => "Google Drive";

        public override string ExampleURL => "https://drive.google.com/file/";

        public override ImportSource Source => ImportSource.GoogleDrive;

        public override Codex SetMetaData(Codex codex)
        {
            worker.ReportProgress(ProgressCounter, new LogEntry(LogEntry.MsgType.Info, $"Connecting to {ImportTitle}"));

            HtmlDocument doc = ScrapeSite(InputURL);
            HtmlNode src = doc?.DocumentNode;

            if (src is null)
            {
                return codex;
            }

            worker.ReportProgress(ProgressCounter, new LogEntry(LogEntry.MsgType.Info, "Fetching Metadata"));

            //Scrape metadata
            codex = SetWebScrapeHeaderMetadata(codex, src);

            //Set known metadata
            codex.Publisher = "Google Drive";

            return codex;
        }

        public override bool FetchCover(Codex codex)
        {
            try
            {
                //cover art is on store page, redirect there by going to /credits which every book has
                HtmlDocument doc = ScrapeSite(codex.SourceURL);
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
                Logger.log.Error(ex.InnerException);
                return false;
            }
        }
    }
}
