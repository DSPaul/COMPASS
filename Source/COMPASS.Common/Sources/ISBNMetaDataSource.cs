using System.Diagnostics;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Infra.Models;
using ImageMagick;
using System.Text.Json.Nodes;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Tools.Logging;

namespace COMPASS.Common.Sources
{
    public class ISBNMetadataSource : MetadataSource
    {
        private readonly IWebService _webService;

        public ISBNMetadataSource(ILogger logger, IPreferencesService preferencesService, IWebService webService) :
            base(logger, preferencesService)
        {
            _webService = webService;
        }
        
        public override MetadataSourceType Type => MetadataSourceType.ISBN;
        public override bool IsValidSource(SourceSet sources) => !String.IsNullOrWhiteSpace(sources.ISBN);

        public override async Task<SourceMetadata> GetMetadata(SourceSet sources, IList<Tag> availableTags, CancellationToken cancellationToken = default)
        {
            Debug.Assert(IsValidSource(sources), "Codex without ISBN was used in ISBN Source");
            
            SourceMetadata metaData = new();
            
            Logger.Info($"Downloading Metadata from openlibrary.org");
            string uri = $"https://openlibrary.org/api/books?bibkeys=ISBN:{sources.ISBN.Trim('-', ' ')}&format=json&jscmd=details";

            JsonNode? openLibraryData = await _webService.GetJsonAsync(uri, cancellationToken);

            if (openLibraryData is not JsonObject openLibraryObject || openLibraryObject.Count == 0)
            {
                string message = $"ISBN {sources.ISBN} was not found on openlibrary.org \n" +
                    $"You can contribute by submitting this book at \n" +
                    $"https://openlibrary.org/books/add";
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
            string fullTitle = details["full_title"]?.GetStringValue() ?? "";
            string title = details["title"]?.GetStringValue() ?? "";
            string subTitle = details["subtitle"]?.GetStringValue() ?? "";

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
                metaData.Authors = authors.Select(item => item?["name"]?.GetStringValue() ?? string.Empty)
                                          .Where(author => author != string.Empty)
                                          .ToList();
            }

            //PageCount
            if (details["pagination"] is JsonNode pagination &&
                int.TryParse(RegexConstants.Numbers().Match(pagination.GetStringValue() ?? string.Empty).Value, out int pageCount))
            {
                metaData.PageCount = pageCount;
            }
            else if (details["number_of_pages"] is JsonNode nrOfPages && nrOfPages.GetIntValue() != null)
            {
                metaData.PageCount = nrOfPages.GetIntValue()!.Value;
            }

            //Publisher
            metaData.Publisher = details["publishers"]?[0]?.GetStringValue() ?? string.Empty;

            //  Description
            metaData.Description = details["description"]?.GetStringValue() ?? string.Empty;

            //Release Date
            if (DateTime.TryParse(details["publish_date"]?.GetStringValue(), out DateTime tempDate))
            {
                metaData.ReleaseDate = tempDate;
            }

            return metaData;
        }

        public override async Task<IMagickImage<byte>?> FetchCover(SourceSet sources, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(sources.ISBN)) return null;
            Logger.Info($"Downloading cover from openlibrary.org");
            try
            {
                string uri = $"https://openlibrary.org/isbn/{sources.ISBN}.json";
                JsonNode? metadata = await _webService.GetJsonAsync(uri, cancellationToken);

                if (metadata is not JsonObject metadataObject || metadataObject.Count == 0)
                {
                    string message = $"ISBN {sources.ISBN} was not found on openlibrary.org \n" +
                        $"You can contribute by submitting this book at \n" +
                        $"https://openlibrary.org/books/add";
                    Logger.Warn($"Could not find ISBN {sources.ISBN} on openlibrary.org");
                    return null;
                }

                string? imgId = metadata["covers"]?[0]?.GetIntValue()?.ToString();
                if (imgId is null) return null;
                string imgURL = $"https://covers.openlibrary.org/b/id/{imgId}.jpg";
                return await _webService.DownloadImageAsync(imgURL, cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                //to be handled by the caller
                throw;
            }
            catch (Exception ex)
            {
                string msg = $"Failed to get cover from OpenLibrary for ISBN {sources.ISBN}";
                Logger.Error(msg, ex);
                return null;
            }
        }

    }
}
