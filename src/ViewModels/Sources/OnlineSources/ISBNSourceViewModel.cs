using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.Windows;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace COMPASS.ViewModels.Sources
{
    public class ISBNSourceViewModel : OnlineSourceViewModel
    {
        public override string ImportTitle => "ISBN";
        public override string ExampleURL => "";
        public override ImportSource Source => ImportSource.ISBN;

        public override Codex SetMetaData(Codex codex)
        {
            worker.ReportProgress(ProgressCounter, new LogEntry(LogEntry.MsgType.Info, "Fetching Data"));
            string uri = $"http://openlibrary.org/api/books?bibkeys=ISBN:{InputURL.Trim('-', ' ')}&format=json&jscmd=details";

            JObject metadata = Task.Run(async () => await Utils.GetJsonAsync(uri)).Result;

            if (!metadata.HasValues)
            {
                string message = $"ISBN {InputURL} was not found on openlibrary.org \n" +
                    $"You can contribute by submitting this book at \n" +
                    $"https://openlibrary.org/books/add";
                worker.ReportProgress(ProgressCounter, new LogEntry(LogEntry.MsgType.Error, message));
                return codex;
            }

            //loading complete
            ProgressCounter++;
            worker.ReportProgress(ProgressCounter, new LogEntry(LogEntry.MsgType.Info, "File loaded, parsing metadata"));

            //Start parsing json
            var details = metadata.First.First.SelectToken("details");
            //Title
            codex.Title = (string)details.SelectToken("title");
            //Authors
            if (details.SelectToken("authors") != null)
            {
                codex.Authors = new(details.SelectToken("authors").Select(item => item.SelectToken("name").ToString()));
            }
            //PageCount
            if (details.SelectToken("pagination") != null)
            {
                codex.PageCount = Int32.Parse(Regex.Match(details.SelectToken("pagination").ToString(), @"\d+").Value);
            }
            codex.PageCount = (int?)details.SelectToken("number_of_pages") ?? codex.PageCount;

            //Publisher
            codex.Publisher = (string)details.SelectToken("publishers[0]") ?? codex.Publisher;

            //  Description
            codex.Description = (string)details.SelectToken("description.value") ?? codex.Description;

            //Release Date
            DateTime tempDate;
            if (DateTime.TryParse((string)details.SelectToken("publish_date"), out tempDate))
                codex.ReleaseDate = tempDate;

            //Other
            codex.ISBN = InputURL;
            codex.Physically_Owned = true;

            return codex;
        }
        public override bool FetchCover(Codex codex)
        {
            try
            {
                string uri = $"https://openlibrary.org/isbn/{codex.ISBN}.json";
                JObject metadata = Task.Run(async () => await Utils.GetJsonAsync(uri)).Result;
                string imgID = (string)metadata.SelectToken("covers[0]");
                string imgURL = $"https://covers.openlibrary.org/b/id/{imgID}.jpg";
                CoverFetcher.SaveCover(imgURL, codex);
                return true;
            }
            catch (Exception ex)
            {
                Logger.log.Error(ex.InnerException);
                return false;
            }
        }

        private ActionCommand _OpenBarcodeScannerCommand;
        public ActionCommand OpenBarcodeScannerCommand => _OpenBarcodeScannerCommand ??= new(OpenBarcodeScanner);
        public void OpenBarcodeScanner()
        {
            BarcodeScanWindow bcScanWindow = new();
            if (bcScanWindow.ShowDialog() == true)
            {
                InputURL = bcScanWindow.DecodedString;
            }
        }
    }
}
