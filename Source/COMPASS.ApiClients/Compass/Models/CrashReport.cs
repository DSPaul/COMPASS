namespace COMPASS.ApiClients.Compass.Models
{
    public class CrashReport
    {
        public required string Version { get; init; }
        public required string OperatingSystem { get; init; }
        public required string Error { get; init; }
    }
}
