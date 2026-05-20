using COMPASS.ApiClients.Compass.Models;

namespace COMPASS.ApiClients.Compass
{
    public interface ICompassApiClient
    {
        Task SubmitCrashReportAsync(CrashReport crashReport);
    }
}
