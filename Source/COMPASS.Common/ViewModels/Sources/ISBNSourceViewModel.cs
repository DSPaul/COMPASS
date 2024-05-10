using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services;
using COMPASS.Common.Tools;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace COMPASS.Common.ViewModels.Sources
{
    public class ISBNSourceViewModel : SourceViewModel
    {
        public override MetaDataSource Source => MetaDataSource.ISBN;
        public override bool IsValidSource(Codex codex) => !String.IsNullOrWhiteSpace(codex.ISBN);

        public override async Task<Codex> GetMetaData(Codex codex)
        {
            // Work on a copy
            codex = new Codex(codex);

            ProgressVM.AddLogEntry(new(Severity.Info, $"Downloading Metadata from openlibrary.org"));
            Debug.Assert(IsValidSource(codex), "Codex without ISBN was used in ISBN Source");
            string uri = $"https://openlibrary.org/api/books?bibkeys=ISBN:{codex.ISBN.Trim('-', ' ')}&format=json&jscmd=details";

            JObject? metadata = await IOService.GetJsonAsync(uri);

            if (metadata is null || !metadata.HasValues)
            {
                string message = $"ISBN {codex.ISBN} was not found on openlibrary.org \n" +
                    $"You can contribute by submitting this book at \n" +
                    $"https://openlibrary.org/books/add";
                ProgressVM.AddLogEntry(new(Severity.Warning, message));
                Logger.Warn($"Could not find ISBN {codex.ISBN} on openlibrary.org");
                return codex;
            }

            // Start parsing json
            var details = metadata.First?.First?.SelectToken("details");
            if (details is null)
            {
                Logger.Warn("Unable to parse metadata from openlibrary");
                return codex;
            }

            // Title
            string fullTitle = details.SelectToken("full_title")?.ToString() ?? "";
            string title = details.SelectToken("title")?.ToString() ?? "";
            string subTitle = details.SelectToken("subtitle")?.ToString() ?? "";

            if (!String.IsNullOrWhiteSpace(fullTitle))
            {
                codex.Title = fullTitle;
            }
            else if (title.Length + subTitle.Length > 0)
            {
                codex.Title = $"{title} {subTitle}";
            }

            //Authors
            if (details.SelectToken("authors") is JToken authors)
            {
                codex.Authors = new(
                    authors.Select(item => item.SelectToken("name")?.ToString() ?? "")
                           .Where(author => author != ""));
            }
            //PageCount
            if (details.SelectToken("pagination") is JToken pagination)
            {
                codex.PageCount = Int32.Parse(Regex.Match(pagination.ToString(), @"\d+").Value);
            }
            codex.PageCount = (int?)details.SelectToken("number_of_pages") ?? codex.PageCount;

            //Publisher
            codex.Publisher = details.SelectToken("publishers[0]")?.ToString() ?? codex.Publisher;

            //  Description
            codex.Description = details.SelectToken("description.value")?.ToString() ?? codex.Description;

            //Release Date
            if (DateTime.TryParse(details.SelectToken("publish_date")?.ToString(), out DateTime tempDate))
            {
                codex.ReleaseDate = tempDate;
            }

            return codex;
        }

        public override async Task<bool> FetchCover(Codex codex)
        {
            if (String.IsNullOrEmpty(codex.ISBN)) return false;
            ProgressVM.AddLogEntry(new(Severity.Info, $"Downloading cover from openlibrary.org"));
            try
            {
                string uri = $"https://openlibrary.org/isbn/{codex.ISBN}.json";
                JObject? metadata = await IOService.GetJsonAsync(uri);

                if (metadata == null || !metadata.HasValues)
                {
                    string message = $"ISBN {codex.ISBN} was not found on openlibrary.org \n" +
                        $"You can contribute by submitting this book at \n" +
                        $"https://openlibrary.org/books/add";
                    ProgressVM.AddLogEntry(new(Severity.Warning, message));
                    Logger.Warn($"Could not find ISBN {codex.ISBN} on openlibrary.org");
                    return false;
                }

                string? imgID = metadata.SelectToken("covers[0]")?.ToString();
                if (imgID is null) return false;
                string imgURL = $"https://covers.openlibrary.org/b/id/{imgID}.jpg";
                await CoverService.SaveCover(imgURL, codex);
                return true;
            }
            catch (Exception ex)
            {
                string msg = $"Failed to get cover from OpenLibrary for ISBN {codex.ISBN}";
                Logger.Error(msg, ex);
                ProgressVM.AddLogEntry(new(Severity.Warning, msg));
                return false;
            }
        }

    }
}
