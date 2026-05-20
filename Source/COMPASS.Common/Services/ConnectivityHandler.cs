using COMPASS.Common.Services.StateManagers;

namespace COMPASS.Common.Services;

/// <summary>
/// A delegating handler that keeps <see cref="ConnectivityManager.IsOnline"/> up to date based on
/// whether HTTP requests through the API clients succeed or fail.
/// On a successful response, we know we are online.
/// On an <see cref="HttpRequestException"/>, we trigger a connection check to determine
/// whether the failure is due to being fully offline.
/// </summary>
public class ConnectivityHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            ConnectivityManager.IsOnline = true;
            return response;
        }
        catch (HttpRequestException)
        {
            await ConnectivityManager.CheckConnection().ConfigureAwait(false);
            throw;
        }
    }
}
