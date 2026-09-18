using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Infra.Tools.Logging;
using COMPASS.Infra.Models.Progress;
using HtmlAgilityPack;
using ImageMagick;
using System.Text.Json.Nodes;

namespace COMPASS.Common.Services;

public class WebService(ILogger logger, IHttpClientFactory httpClientFactory, ConnectivityManager connectivityManager) : IWebService
{
    public const string BrowserHttpClient = "browser";
    public const string ConnectionCheckHttpClient = "connection-check";

    private readonly ConnectivityManager _connectivityManager = connectivityManager;

    //Download data and put it in a byte[]
    public async Task<byte[]> DownloadFileAsync(string uri, IProgress<IProgressReport>? progress = null, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient(BrowserHttpClient);

        if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri? _))
            throw new InvalidOperationException("URI is invalid.");
        try
        {
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            _connectivityManager.IsOnline = true;

            long? totalBytes = response.Content.Headers.ContentLength;

            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var memoryStream = new MemoryStream();

            var buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer).ConfigureAwait(false)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                totalRead += bytesRead;

                if (progress is not null && totalBytes > 0)
                {
                    progress.Report(ProgressReports.XOutOfY(totalRead, totalBytes.Value));
                }
            }

            return memoryStream.ToArray();
        }
        catch (HttpRequestException ex)
        {
            //might mean we are offline, but not necessarily so check to inform user
            await _connectivityManager.CheckConnection();
            logger.Error($"Failed to fetch data at {uri}", ex);
            return [];
        }
        catch(OperationCanceledException)
        {
            //caller should handle cancelation
            throw; 
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to fetch data at {uri}", ex);
            return [];
        }
    }

    public async Task<JsonNode?> GetJsonAsync(string uri, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient();

        JsonNode? json = null;

        if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri? _))
            throw new InvalidOperationException("URI is invalid.");
        try
        {
            HttpResponseMessage response = await client.GetAsync(uri, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                _connectivityManager.IsOnline = true;
                string data = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                json = JsonNode.Parse(data);
            }
        }
        catch (Exception ex)
        {
            //might mean we are offline, but not necessarily so check to inform user
            await _connectivityManager.CheckConnection();
            logger.Error($"Failed to fetch data at {uri}", ex);
        }

        return json;
    }

    public async Task<MagickImage?> DownloadImageAsync(string imgURL, IProgress<IProgressReport>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var imgBytes = await DownloadFileAsync(imgURL, progress, cancellationToken).ConfigureAwait(false);
            _connectivityManager.IsOnline = true;
            return new(imgBytes);
        }
        catch (OperationCanceledException)
        {
            //caller should handle cancelation
            throw;
        }
        catch (Exception ex)
        {
            //might mean we are offline, but not necessarily so check to inform user
            await _connectivityManager.CheckConnection();
            logger.Error($"Failed to download cover image from {imgURL}", ex);
            return null;
        }
    }

    public async Task<HtmlDocument?> ScrapeSite(string url, CancellationToken cancellationToken = default)
    {
        HtmlWeb web = new();
        HtmlDocument doc;

        try
        {
            doc = await  web.LoadFromWebAsync(url, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            //fails if URL could not be loaded
            //might mean we are offline, but not necessarily so check to inform user
            await _connectivityManager.CheckConnection();
            logger.Error($"Could not load {url}", ex);
            return null;
        }

        if (doc.ParsedText is null || doc.DocumentNode is null)
        {
            logger.Error($"{url} does not have any content", new Exception($"Failed to reach {url}"));
            return null;
        }
        else
        {
            _connectivityManager.IsOnline = true;
            return doc;
        }
    }

}
