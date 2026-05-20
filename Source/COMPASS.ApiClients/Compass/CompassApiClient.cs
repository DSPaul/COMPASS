using System.Text.Json;
using COMPASS.ApiClients.Compass.Models;

namespace COMPASS.ApiClients.Compass
{
    internal class CompassApiClient(IHttpClientFactory httpClientFactory) : ICompassApiClient
    {
        internal const string HttpClientName = "compass-api";

        //Api Key is shared by all instances of COMPASS, the api is open
        //it's only purpose is to filter out blind spam from bots, so it's fine to 'leak' it
        internal const string ApiKey = "uwr2BswLryEsaXvjXEuumN6rwtKIGSBGv002APVgwDN4UCcb6LGKpFyuxAM9FuV9Ai030vQVc9NXL7ekKiQ0TJ9si53jvGxyiix5EVVqvqJeZkZ7wcG4hYtqdkuJNaFE";
        //private const string ApiDomain = "https://api.compassapp.info"; //custom domain starts at 10$/month on azure, I'm not paying for that
        private const string ApiDomain = "https://compass-api-pds.azurewebsites.net";

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public async Task SubmitCrashReportAsync(CrashReport crashReport)
        {
            var client = httpClientFactory.CreateClient(HttpClientName);
            string json = JsonSerializer.Serialize(crashReport, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            using var response = await client.PostAsync($"{ApiDomain}/submit/crash", content).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
    }
}
