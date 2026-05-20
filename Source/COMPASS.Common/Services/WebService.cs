using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels;
using COMPASS.Infra.Interfaces.Services;
using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Enums;
using HtmlAgilityPack;
using ImageMagick;
using System.Text.Json.Nodes;

namespace COMPASS.Common.Services;

public class WebService(ILogger logger, IHttpClientFactory httpClientFactory) : IWebService
{
    public const string BrowserHttpClient = "browser";
    public const string ConnectionCheckHttpClient = "connection-check";

    //Download data and put it in a byte[]
    public async Task<byte[]> DownloadFileAsync(string uri)
    {
        var client = httpClientFactory.CreateClient(BrowserHttpClient);

        if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri? _))
            throw new InvalidOperationException("URI is invalid.");
        try
        {
            var data = await client.GetByteArrayAsync(uri).ConfigureAwait(false);
            ConnectivityManager.IsOnline = true;
            return data;
        }
        catch (HttpRequestException ex)
        {
            //might mean we are offline, but not necessarily so check to inform user
            await ConnectivityManager.CheckConnection();
            logger.Error($"Failed to fetch data at {uri}", ex);
            return [];
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to fetch data at {uri}", ex);
            return [];
        }
    }

    public async Task<JsonNode?> GetJsonAsync(string uri)
    {
        var client = httpClientFactory.CreateClient();

        JsonNode? json = null;

        if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri? _))
            throw new InvalidOperationException("URI is invalid.");
        try
        {
            HttpResponseMessage response = await client.GetAsync(uri).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                ConnectivityManager.IsOnline = true;
                string data = await response.Content.ReadAsStringAsync();
                json = JsonNode.Parse(data);
            }
        }
        catch (Exception ex)
        {
            //might mean we are offline, but not necessarily so check to inform user
            await ConnectivityManager.CheckConnection();
            logger.Error($"Failed to fetch data at {uri}", ex);
        }

        return json;
    }

    public async Task<MagickImage?> DownloadImageAsync(string imgURL)
    {
        try
        {
            var imgBytes = await DownloadFileAsync(imgURL).ConfigureAwait(false);
            ConnectivityManager.IsOnline = true;
            return new(imgBytes);
        }
        catch (Exception ex)
        {
            //might mean we are offline, but not necessarily so check to inform user
            await ConnectivityManager.CheckConnection();
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
            doc = await  web.LoadFromWebAsync(url).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            //fails if URL could not be loaded
            //might mean we are offline, but not necessarily so check to inform user
            await ConnectivityManager.CheckConnection();
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
            ConnectivityManager.IsOnline = true;
            return doc;
        }
    }

    }