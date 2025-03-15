using CommandLine;
using COMPASS.Common.Models;

namespace COMPASS.Common.Services;

public static class CmdLineArgumentService
{
    public static Options? Args { get; set; }

    public static void ParseArgs(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args).WithParsed(opts => Args = opts);
    }
    
    public class Options
    {
        [Option(longName: Constants.CmdArgNotifyCrashed)]
        public string? CrashMessage { get; set; }
    }
}