using COMPASS.Models;
using HtmlAgilityPack;
using System;

namespace COMPASS.ViewModels
{
    public class DndBeyondSourceViewModel : OnlineSourceViewModel
    {
        public override string ImportTitle => "D&D Beyond";

        public override string ExampleURL => "https://www.dndbeyond.com/sources/";

        public override Sources Source => Sources.DnDBeyond;

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
    }
}
