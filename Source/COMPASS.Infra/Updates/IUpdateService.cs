namespace COMPASS.Infra.Updates
{
    public interface IUpdateService
    {
        Task<List<Update>> CheckForUpdates(bool includePrerelease);
        Task OnUpdatesFound(IList<Update> updates);
        Task HandleUpdate(Update update);
    }
}
