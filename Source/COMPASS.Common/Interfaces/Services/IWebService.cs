using HtmlAgilityPack;
using ImageMagick;
using Newtonsoft.Json.Linq;

namespace COMPASS.Common.Interfaces.Services;

public interface IWebService
{
    Task<byte[]> DownloadFileAsync(string uri);

    Task<JObject?> GetJsonAsync(string uri);

    Task<MagickImage?> DownloadImageAsync(string imgURL);

    Task<HtmlDocument?> ScrapeSite(string url);

    bool CheckConnection(string? url = null);
}