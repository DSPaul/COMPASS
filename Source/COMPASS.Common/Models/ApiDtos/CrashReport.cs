using COMPASS.Common.Tools;
using System;

namespace COMPASS.Common.Models.ApiDtos
{
    public class CrashReport
    {
        public CrashReport(Exception exception)
        {
            Error = exception.ToString();
        }

        public string Version => Reflection.Version;

        public string OperatingSystem { get; } = System.Runtime.InteropServices.RuntimeInformation.OSDescription;

        public string Error { get; set; }
    }
}
