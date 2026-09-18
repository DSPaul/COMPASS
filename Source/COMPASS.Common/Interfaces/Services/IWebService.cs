using COMPASS.Infra.Models.Progress;
using HtmlAgilityPack;
using ImageMagick;
using System.Text.Json.Nodes;

namespace COMPASS.Common.Interfaces.Services;

public interface IWebService
{
    Task<byte[]> DownloadFileAsync(string uri, IProgress<IProgressReport>? progress = null, CancellationToken cancellationToken = default);

    Task<JsonNode?> GetJsonAsync(string uri, CancellationToken cancellationToken = default);

    Task<MagickImage?> DownloadImageAsync(string imgURL, IProgress<IProgressReport>? progress = null, CancellationToken cancellationToken = default);

    Task<HtmlDocument?> ScrapeSite(string url, CancellationToken cancellationToken = default);
}