using COMPASS.Infra.Models;

namespace COMPASS.Infra.Interfaces.Services
{
    public interface IUpdateService
    {
        Task<List<Update>> CheckForUpdates();

        Task HandleUpdates(IList<Update> updates);
    }
}
