using COMPASS.Common.Services;

namespace COMPASS.Common.Models.ApiDtos
{
    public class CrashReport
    {
        public CrashReport(string exceptionMessage)
        {
            Error = exceptionMessage;
        }

        public string Version => ApplicationService.Version;

        public string OperatingSystem { get; } = System.Runtime.InteropServices.RuntimeInformation.OSDescription;

        public string Error { get; set; }
    }
}
