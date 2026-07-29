using COMPASS.Infra.Models.Updates;

namespace COMPASS.Infra.Interfaces.Services
{
    public interface IUpdateService
    {
        Task<List<Update>> CheckForUpdates(bool includePrerelease);
        Task OnUpdatesFound(IList<Update> updates);
        Task HandleUpdate(Update update);
    }
}
