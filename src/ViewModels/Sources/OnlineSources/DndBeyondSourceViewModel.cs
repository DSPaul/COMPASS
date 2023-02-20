using COMPASS.Models;
using COMPASS.Tools;
using HtmlAgilityPack;
using System;

namespace COMPASS.ViewModels.Sources
{
    public class DndBeyondSourceViewModel : OnlineSourceViewModel
    {
        public override string ImportTitle => "D&D Beyond";

        public override string ExampleURL => "https://www.dndbeyond.com/sources/";

        public override ImportSource Source => ImportSource.DnDBeyond;

        public override Codex SetMetaData(Codex codex)
        {
            //Scrape metadata by going to storepage, get to storepage by using that /credits redirects there
            worker.ReportProgress(ProgressCounter, new LogEntry(LogEntry.MsgType.Info, $"Connecting to {ImportTitle}"));
            HtmlDocument doc = ScrapeSite(String.Concat(InputURL, "/credits"));
            HtmlNode src = doc?.DocumentNode;

            if (src is null)
            {
                return codex;
            }

            worker.ReportProgress(ProgressCounter, new LogEntry(LogEntry.MsgType.Info, "Fetching Metadata"));

            //Scrape headers
            codex = SetWebScrapeHeaderMetadata(codex, src);

            //Set known metadata
            codex.Publisher = "D&D Beyond";
            codex.Authors = new() { "Wizards of the Coast" };

            return codex;
        }

        public override bool FetchCover(Codex codex)
        {
            try
            {
                //cover art is on store page, redirect there by going to /credits which every book has
                HtmlDocument doc = ScrapeSite(String.Concat(codex.SourceURL, "/credits"));
                HtmlNode src = doc?.DocumentNode;
                if (src is null) return false;

                string imgURL = src.SelectSingleNode("//img[@class='product-hero-avatar__image']").GetAttributeValue("content", String.Empty);

                //download the file
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
