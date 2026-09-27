namespace COMPASS.Infra.Application
{
    public interface IApplicationService
    {
        string Version { get; }
        bool FirstRunSinceUpdate { get; set; }

        void Restart(bool keepArgs);
        void Shutdown();
    }
}
