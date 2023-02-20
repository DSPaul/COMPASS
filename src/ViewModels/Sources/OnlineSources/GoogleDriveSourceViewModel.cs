using COMPASS.Models;
using HtmlAgilityPack;

namespace COMPASS.ViewModels
{
    public class GoogleDriveSourceViewModel : OnlineSourceViewModel
    {
        public override string ImportTitle => "Google Drive";

        public override string ExampleURL => "https://drive.google.com/file/";

        public override Sources Source => Sources.GoogleDrive;

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
    }
}
