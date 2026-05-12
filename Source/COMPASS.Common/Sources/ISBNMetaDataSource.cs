using System.Diagnostics;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Enums;
using COMPASS.Infra.Tools;
using ImageMagick;
using System.Text.Json.Nodes;

namespace COMPASS.Common.Sources
{
    public class ISBNMetaDataSource : MetaDataSource
    {
        private readonly IWebService _webService;

        public ISBNMetaDataSource(CodexCollection targetCollection) :
            base(targetCollection)
        {
            _webService = ServiceResolver.Resolve<IWebService>();
        }
        
        public override MetaDataSourceType Type => MetaDataSourceType.ISBN;
        public override bool IsValidSource(SourceSet sources) => !String.IsNullOrWhiteSpace(sources.ISBN);

        public override async Task<SourceMetaData> GetMetaData(SourceSet sources)
        {
            Debug.Assert(IsValidSource(sources), "Codex without ISBN was used in ISBN Source");
            
            SourceMetaData metaData = new();
            
            ProgressVM.AddLogEntry(new(Severity.Info, $"Downloading Metadata from openlibrary.org"));
            string uri = $"https://openlibrary.org/api/books?bibkeys=ISBN:{sources.ISBN.Trim('-', ' ')}&format=json&jscmd=details";

            JsonNode? openLibraryData = await _webService.GetJsonAsync(uri);

            if (openLibraryData is not JsonObject openLibraryObject || openLibraryObject.Count == 0)
            {
                string message = $"ISBN {sources.ISBN} was not found on openlibrary.org \n" +
                    $"You can contribute by submitting this book at \n" +
                    $"https://openlibrary.org/books/add";
                ProgressVM.AddLogEntry(new(Severity.Warning, message));
                Logger.Warn($"Could not find ISBN {sources.ISBN} on openlibrary.org");
                return metaData;
            }

            // Start parsing json
            // The response is a dictionary keyed by "ISBN:xxxx", navigate to first entry's "details"
            JsonNode? details = openLibraryObject.First().Value?["details"];
            if (details is null)
            {
                Logger.Warn("Unable to parse metadata from openlibrary");
                return metaData;
            }

            // Title
            string fullTitle = details["full_title"]?.GetValue<string>() ?? "";
            string title = details["title"]?.GetValue<string>() ?? "";
            string subTitle = details["subtitle"]?.GetValue<string>() ?? "";

            if (!string.IsNullOrWhiteSpace(fullTitle))
            {
                metaData.Title = fullTitle;
            }
            else if (title.Length + subTitle.Length > 0)
            {
                metaData.Title = $"{title} {subTitle}".Trim();
            }

            //Authors
            if (details["authors"] is JsonArray authors)
            {
                metaData.Authors = authors.Select(item => item?["name"]?.GetValue<string>() ?? string.Empty)
                                          .Where(author => author != string.Empty)
                                          .ToList();
            }

            //PageCount
            int pageCount = 0;
            if (details["pagination"] is JsonNode pagination &&
                int.TryParse(RegexConstants.Numbers().Match(pagination.GetValue<string>()).Value, out pageCount))
            {
                metaData.PageCount = pageCount;
            }
            else if (details["number_of_pages"] is JsonNode nrOfPages &&
                     int.TryParse(nrOfPages.GetValue<string>(), out pageCount))
            {
                metaData.PageCount = pageCount;
            }

            //Publisher
            metaData.Publisher = details["publishers"]?[0]?.GetValue<string>() ?? string.Empty;

            //  Description
            metaData.Description = details["description"]?["value"]?.GetValue<string>() ?? string.Empty;

            //Release Date
            if (DateTime.TryParse(details["publish_date"]?.GetValue<string>(), out DateTime tempDate))
            {
                metaData.ReleaseDate = tempDate;
            }

            return metaData;
        }

        public override async Task<IMagickImage<byte>?> FetchCover(SourceSet sources)
        {
            if (string.IsNullOrEmpty(sources.ISBN)) return null;
            ProgressVM.AddLogEntry(new(Severity.Info, $"Downloading cover from openlibrary.org"));
            try
            {
                string uri = $"https://openlibrary.org/isbn/{sources.ISBN}.json";
                JsonNode? metadata = await _webService.GetJsonAsync(uri);

                if (metadata is not JsonObject metadataObject || metadataObject.Count == 0)
                {
                    string message = $"ISBN {sources.ISBN} was not found on openlibrary.org \n" +
                        $"You can contribute by submitting this book at \n" +
                        $"https://openlibrary.org/books/add";
                    ProgressVM.AddLogEntry(new(Severity.Warning, message));
                    Logger.Warn($"Could not find ISBN {sources.ISBN} on openlibrary.org");
                    return null;
                }

                string? imgId = metadata["covers"]?[0]?.GetValue<int>().ToString();
                if (imgId is null) return null;
                string imgURL = $"https://covers.openlibrary.org/b/id/{imgId}.jpg";
                return await _webService.DownloadImageAsync(imgURL);
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
