using CommandLine;

namespace COMPASS.Infra.Application;

public static class CmdLineArgumentService
{
    public const string CMD_ARG_NotifyCrashed = "notify_crashed";

    public static Options? Args { get; set; }

    public static void ParseArgs(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args).WithParsed(opts => Args = opts);
    }
    
    public class Options
    {
        [Option(longName: CMD_ARG_NotifyCrashed)]
        public string? CrashMessage { get; set; }
    }
}