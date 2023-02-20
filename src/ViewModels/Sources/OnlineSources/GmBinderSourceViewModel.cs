using COMPASS.Models;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;

namespace COMPASS.ViewModels
{
    public class GmBinderSourceViewModel : OnlineSourceViewModel
    {
        public override string ImportTitle => "GM Binder";

        public override string ExampleURL => "https://www.gmbinder.com/share/";

        public override Sources Source => Sources.GmBinder;

        public override Codex SetMetaData(Codex codex)
        {
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
            codex.Publisher = "GM Binder";

            //get pagecount
            HtmlNode previewDiv = doc.GetElementbyId("preview");
            IEnumerable<HtmlNode> pages = previewDiv.ChildNodes.Where(node => node.Id.Contains('p'));
            codex.PageCount = pages.Count();

            return codex;
        }
    }
}
