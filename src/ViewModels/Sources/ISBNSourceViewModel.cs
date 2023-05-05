using COMPASS.Models;
using COMPASS.Tools;
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

            ProgressVM.AddLogEntry(new(LogEntry.MsgType.Info, $"Downloading Metadata from openlibrary.org"));

            string uri = $"http://openlibrary.org/api/books?bibkeys=ISBN:{codex.ISBN.Trim('-', ' ')}&format=json&jscmd=details";

            JObject metadata = await Utils.GetJsonAsync(uri);

            if (!metadata.HasValues)
            {
                string message = $"ISBN {codex.ISBN} was not found on openlibrary.org \n" +
                    $"You can contribute by submitting this book at \n" +
                    $"https://openlibrary.org/books/add";
                ProgressVM.AddLogEntry(new(LogEntry.MsgType.Warning, message));
                Logger.Warn($"Could not find ISBN {codex.ISBN} on openlibrary.org");
                return codex;
            }

            // Start parsing json
            var details = metadata.First.First.SelectToken("details");

            // Title
            string fullTitle = (string)details.SelectToken("full_title");
            string title = (string)details.SelectToken("title") ?? "";
            string subTitle = (string)details.SelectToken("subtitle") ?? "";
            codex.Title = !String.IsNullOrWhiteSpace(fullTitle) ? fullTitle : $"{title} {subTitle}";

            //Authors
            if (details.SelectToken("authors") is not null)
            {
                codex.Authors = new(details.SelectToken("authors").Select(item => item.SelectToken("name").ToString()));
            }
            //PageCount
            if (details.SelectToken("pagination") is not null)
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

            return codex;
        }

        public override async Task<bool> FetchCover(Codex codex)
        {
            if (String.IsNullOrEmpty(codex.ISBN)) return false;
            ProgressVM.AddLogEntry(new(LogEntry.MsgType.Info, $"Downloading cover from openlibrary.org"));
            try
            {
                string uri = $"https://openlibrary.org/isbn/{codex.ISBN}.json";
                JObject metadata = await Utils.GetJsonAsync(uri);

                if (!metadata.HasValues)
                {
                    string message = $"ISBN {codex.ISBN} was not found on openlibrary.org \n" +
                        $"You can contribute by submitting this book at \n" +
                        $"https://openlibrary.org/books/add";
                    ProgressVM.AddLogEntry(new(LogEntry.MsgType.Warning, message));
                    Logger.Warn($"Could not find ISBN {codex.ISBN} on openlibrary.org");
                    return false;
                }

                string imgID = (string)metadata.SelectToken("covers[0]");
                string imgURL = $"https://covers.openlibrary.org/b/id/{imgID}.jpg";
                CoverFetcher.SaveCover(imgURL, codex);
                return true;
            }
            catch (Exception ex)
            {
                string msg = $"Failed to get cover from OpenLibrary for ISBN {codex.ISBN}";
                Logger.Error(msg, ex);
                ProgressVM.AddLogEntry(new(LogEntry.MsgType.Warning, msg));
                return false;
            }
        }

    }
}
