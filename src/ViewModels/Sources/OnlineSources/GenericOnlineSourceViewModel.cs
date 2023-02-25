using COMPASS.Models;
using HtmlAgilityPack;

namespace COMPASS.ViewModels.Sources
{
    public class GenericOnlineSourceViewModel : OnlineSourceViewModel
    {
        public override string ImportTitle => "Any URL";

        public override string ExampleURL => "https://";

        public override ImportSource Source => ImportSource.GenericURL;

        public override bool FetchCover(Codex codex) => false;
        public override Codex SetMetaData(Codex codex)
        {
            worker.ReportProgress(ProgressCounter, new LogEntry(LogEntry.MsgType.Info, $"Connecting to {InputURL}"));

            HtmlDocument doc = ScrapeSite(InputURL);
            HtmlNode src = doc?.DocumentNode;

            if (src is null)
            {
                return codex;
            }

            worker.ReportProgress(ProgressCounter, new LogEntry(LogEntry.MsgType.Info, "Fetching Metadata"));
            //Scrape metadata
            codex = SetWebScrapeHeaderMetadata(codex, src);
            return codex;
        }
    }
}
