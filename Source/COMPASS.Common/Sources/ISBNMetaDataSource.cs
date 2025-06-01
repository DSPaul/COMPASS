using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;
using ImageMagick;
using Newtonsoft.Json.Linq;

namespace COMPASS.Common.Sources
{
    public class ISBNMetaDataSource : MetaDataSource
    {
        public override MetaDataSourceType Type => MetaDataSourceType.ISBN;
        public override bool IsValidSource(SourceSet sources) => !String.IsNullOrWhiteSpace(sources.ISBN);

        public override async Task<SourceMetaData> GetMetaData(SourceSet sources)
        {
            Debug.Assert(IsValidSource(sources), "Codex without ISBN was used in ISBN Source");
            
            SourceMetaData metaData = new();
            
            ProgressVM.AddLogEntry(new(Severity.Info, $"Downloading Metadata from openlibrary.org"));
            string uri = $"https://openlibrary.org/api/books?bibkeys=ISBN:{sources.ISBN.Trim('-', ' ')}&format=json&jscmd=details";

            JObject? openLibraryData = await IOService.GetJsonAsync(uri);

            if (openLibraryData is null || !openLibraryData.HasValues)
            {
                string message = $"ISBN {sources.ISBN} was not found on openlibrary.org \n" +
                    $"You can contribute by submitting this book at \n" +
                    $"https://openlibrary.org/books/add";
                ProgressVM.AddLogEntry(new(Severity.Warning, message));
                Logger.Warn($"Could not find ISBN {sources.ISBN} on openlibrary.org");
                return metaData;
            }

            // Start parsing json
            JToken? details = openLibraryData.First?.First?.SelectToken("details");
            if (details is null)
            {
                Logger.Warn("Unable to parse metadata from openlibrary");
                return metaData;
            }

            // Title
            string fullTitle = details.SelectToken("full_title")?.ToString() ?? "";
            string title = details.SelectToken("title")?.ToString() ?? "";
            string subTitle = details.SelectToken("subtitle")?.ToString() ?? "";
            

            if (!string.IsNullOrWhiteSpace(fullTitle))
            {
                metaData.Title = fullTitle;
            }
            else if (title.Length + subTitle.Length > 0)
            {
                metaData.Title = $"{title} {subTitle}";
            }

            //Authors
            if (details.SelectToken("authors") is JToken authors)
            {
                metaData.Authors = authors.Select(item => item.SelectToken("name")?.ToString() ?? string.Empty)
                                          .Where(author => author != string.Empty)
                                          .ToList();
            }
            //PageCount
            int pageCount = 0;
            if (details.SelectToken("pagination") is JToken pagination &&
                int.TryParse(Constants.RegexNumbersOnly().Match(pagination.ToString()).Value, out pageCount))
            {
                metaData.PageCount = pageCount;
            }
            else if (details.SelectToken("number_of_pages") is JToken nrOfPages &&
                     int.TryParse(nrOfPages.ToString(), out pageCount))
            {
                metaData.PageCount = pageCount;
            }

            //Publisher
            metaData.Publisher = details.SelectToken("publishers[0]")?.ToString() ?? string.Empty;

            //  Description
            metaData.Description = details.SelectToken("description.value")?.ToString() ?? string.Empty;

            //Release Date
            if (DateTime.TryParse(details.SelectToken("publish_date")?.ToString(), out DateTime tempDate))
            {
                metaData.ReleaseDate = tempDate;
            }

            return metaData;
        }

        public override async Task<IMagickImage?> FetchCover(SourceSet sources)
        {
            if (string.IsNullOrEmpty(sources.ISBN)) return null;
            ProgressVM.AddLogEntry(new(Severity.Info, $"Downloading cover from openlibrary.org"));
            try
            {
                string uri = $"https://openlibrary.org/isbn/{sources.ISBN}.json";
                JObject? metadata = await IOService.GetJsonAsync(uri);

                if (metadata is not { HasValues: true })
                {
                    string message = $"ISBN {sources.ISBN} was not found on openlibrary.org \n" +
                        $"You can contribute by submitting this book at \n" +
                        $"https://openlibrary.org/books/add";
                    ProgressVM.AddLogEntry(new(Severity.Warning, message));
                    Logger.Warn($"Could not find ISBN {sources.ISBN} on openlibrary.org");
                    return null;
                }

                string? imgId = metadata.SelectToken("covers[0]")?.ToString();
                if (imgId is null) return null;
                string imgURL = $"https://covers.openlibrary.org/b/id/{imgId}.jpg";
                return await IOService.DownloadImageAsync(imgURL);
            }
            catch (Exception ex)
            {
                string msg = $"Failed to get cover from OpenLibrary for ISBN {sources.ISBN}";
                Logger.Error(msg, ex);
                ProgressVM.AddLogEntry(new(Severity.Warning, msg));
                return null;
            }
        }

    }
}
