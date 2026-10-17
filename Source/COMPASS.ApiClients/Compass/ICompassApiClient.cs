using COMPASS.ApiClients.Compass.Models;

namespace COMPASS.ApiClients.Compass
{
    public interface ICompassApiClient
    {
        public const string HttpClientName = "compass-api";

        Task SubmitCrashReportAsync(CrashReport crashReport);
    }
}
