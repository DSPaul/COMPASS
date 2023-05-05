using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.ViewModels.Import;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace COMPASS.ViewModels.Sources
{
    public class ISBNSourceViewModel : SourceViewModel
    {
        public ISBNSourceViewModel() : base() { }
        public ISBNSourceViewModel(CodexCollection targetCollection) : base(targetCollection) { }

        public override MetaDataSource Source => MetaDataSource.ISBN;
        public override bool IsValidSource(Codex codex) => !String.IsNullOrWhiteSpace(codex.ISBN);

        public override async Task<Codex> GetMetaData(Codex codex)
        {
            // Work on a copy
            codex = new Codex(codex);

            if (!ImportViewModel.Stealth)
            {
                ProgressVM.AddLogEntry(new LogEntry(LogEntry.MsgType.Info, "Fetching Data"));
            }

            string uri = $"http://openlibrary.org/api/books?bibkeys=ISBN:{codex.ISBN.Trim('-', ' ')}&format=json&jscmd=details";

            JObject metadata = await Utils.GetJsonAsync(uri);

            if (!metadata.HasValues)
            {
                string message = $"ISBN {codex.ISBN} was not found on openlibrary.org \n" +
                    $"You can contribute by submitting this book at \n" +
                    $"https://openlibrary.org/books/add";
                ProgressVM.AddLogEntry(new(LogEntry.MsgType.Error, message));
                Logger.Warn($"Could not find ISBN {codex.ISBN} on openlibrary.org", new Exception());
                return codex;
            }

            if (!ImportViewModel.Stealth)
            {
                //loading complete
                ProgressVM.AddLogEntry(new(LogEntry.MsgType.Info, "File loaded, parsing metadata"));
            }

            // Start parsing json
            var details = metadata.First.First.SelectToken("details");
            // Title
            if (!String.IsNullOrWhiteSpace((string)details.SelectToken("full_title")))
            {
                codex.Title = (string)details.SelectToken("full_title");
            }
            else
            {
                codex.Title = (string)details.SelectToken("title") ?? codex.Title;
                if (!String.IsNullOrWhiteSpace((string)details.SelectToken("subtitle")))
                {
                    codex.Title += " ";
                    codex.Title += (string)details.SelectToken("subtitle");
                }
            }

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

            MainViewModel.CollectionVM.FilterVM.PopulateMetaDataCollections();
            MainViewModel.CollectionVM.FilterVM.ReFilter();

            return codex;
        }

        public override async Task<bool> FetchCover(Codex codex)
        {
            if (String.IsNullOrEmpty(codex.ISBN)) return false;
            try
            {
                string uri = $"https://openlibrary.org/isbn/{codex.ISBN}.json";
                JObject metadata = await Utils.GetJsonAsync(uri);
                string imgID = (string)metadata.SelectToken("covers[0]");
                string imgURL = $"https://covers.openlibrary.org/b/id/{imgID}.jpg";
                CoverFetcher.SaveCover(imgURL, codex);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get cover from OpenLibrary", ex);
                return false;
            }
        }

    }
}
