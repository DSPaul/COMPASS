using COMPASS.Common.Tools;

namespace COMPASS.Common.Models.ApiDtos
{
    public class CrashReport
    {
        public CrashReport(string exceptionMessage)
        {
            Error = exceptionMessage;
        }

        public string Version => Reflection.Version;

        public string OperatingSystem { get; } = System.Runtime.InteropServices.RuntimeInformation.OSDescription;

        public string Error { get; set; }
    }
}
