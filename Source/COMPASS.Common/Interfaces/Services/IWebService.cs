using HtmlAgilityPack;
using ImageMagick;
using System.Text.Json.Nodes;

namespace COMPASS.Common.Interfaces.Services;

public interface IWebService
{
    Task<byte[]> DownloadFileAsync(string uri);

    Task<JsonNode?> GetJsonAsync(string uri);

    Task<MagickImage?> DownloadImageAsync(string imgURL);

    Task<HtmlDocument?> ScrapeSite(string url);
}