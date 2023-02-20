using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.Windows;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace COMPASS.ViewModels
{
    public class ISBNSourceViewModel : OnlineSourceViewModel
    {
        public override string ImportTitle => "ISBN";
        public override string ExampleURL => "";
        public override Sources Source => Sources.ISBN;

        public new void ImportURL(object sender, DoWorkEventArgs e)
        {
            Codex newCodex = new(MainViewModel.CollectionVM.CurrentCollection);

            newCodex = SetMetaData(newCodex);

            //Scraping complete
            ProgressCounter++;
            worker.ReportProgress(ProgressCounter, new LogEntry(LogEntry.MsgType.Info, "Metadata loaded. Fetching cover art."));

            //Get Cover Art
            CoverFetcher.GetCoverFromISBN(newCodex);

            //add file to cc
            MainViewModel.CollectionVM.CurrentCollection.AllCodices.Add(newCodex);

            //import done
            ProgressCounter++;
            worker.ReportProgress(ProgressCounter);
        }

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
