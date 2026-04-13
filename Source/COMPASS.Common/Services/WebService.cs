using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.ViewModels;
using HtmlAgilityPack;
using ImageMagick;
using Newtonsoft.Json.Linq;

namespace COMPASS.Common.Services;

public class WebService(ILogger logger) : IWebService
{
    //Download data and put it in a byte[]
    public async Task<byte[]> DownloadFileAsync(string uri)
    {
        using HttpClient client = new();
        // Set headers to mimic a browser, gets around some auth issues
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.93 Safari/537.36");

        if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri? _))
            throw new InvalidOperationException("URI is invalid.");
        try
        {
            return await client.GetByteArrayAsync(uri).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to fetch data at {uri}", ex);
            return [];
        }
    }

    public async Task<JObject?> GetJsonAsync(string uri)
    {
        using HttpClient client = new();

        JObject? json = null;

        if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri? _))
            throw new InvalidOperationException("URI is invalid.");
        try
        {
            HttpResponseMessage response = await client.GetAsync(uri).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var data = response.Content.ReadAsStringAsync();
                json = JObject.Parse(data.Result);
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to fetch data at {uri}", ex);
        }

        return json;
    }

    public async Task<MagickImage?> DownloadImageAsync(string imgURL)
    {
        try
        {
            var imgBytes = await DownloadFileAsync(imgURL).ConfigureAwait(false);
            return new(imgBytes);
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to download cover image from {imgURL}", ex);
            return null;
        }
    }

    public async Task<HtmlDocument?> ScrapeSite(string url)
    {
        HtmlWeb web = new();
        HtmlDocument doc;

        var progressVM = ProgressViewModel.GetInstance();

        try
        {
            doc = await Task.Run(() => web.Load(url)).ConfigureAwait(false);
        }

        catch (Exception ex)
        {
            //fails if URL could not be loaded
            progressVM.AddLogEntry(new(Severity.Error, ex.Message));
            logger.Error($"Could not load {url}", ex);
            return null;
        }

        if (doc.ParsedText is null || doc.DocumentNode is null)
        {
            LogEntry entry = new(Severity.Error, $"Failed to reach {url}");
            progressVM.AddLogEntry(entry);
            logger.Error($"{url} does not have any content", new Exception());
            return null;
        }
        else
        {
            return doc;
        }
    }

    //check internet connection
    private bool _showedOfflineWarning = false;

    public bool CheckConnection(string? url = null)
    {
        bool generalCheck = url == null;
        url ??= @"https://google.com";

        try
        {
            using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(3) };
            using HttpResponseMessage reply = client.Send(new HttpRequestMessage(HttpMethod.Head, url));
            if (!reply.IsSuccessStatusCode) return false;
            if (_showedOfflineWarning)
            {
                logger.Info("Internet connection restored");
                _showedOfflineWarning = false;
            }

            return true;
        }
        catch (Exception ex)
        {
            if (!_showedOfflineWarning)
            {
                if (generalCheck)
                {
                    logger.Warn("COMPASS is oflline, some features might not work.");
                }
                else
                {
                    logger.Warn($"Could not reach {url}", ex);
                }
            }

            _showedOfflineWarning = true;
        }

        return false;
    }
}