using System.Diagnostics;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Services.FileSystem;

namespace COMPASS.Linux.Services;

public class IOService(IFilesService filesService, ILogger logger) : IOServiceBase(filesService, logger)
{
    public override void ShowInExplorer(string filePath)
    {
        if (!Path.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", filePath);
        }
        
        var directory = Path.GetDirectoryName(filePath);

        // Try common Linux file managers in order of preference
        var attempts = new[]
        {
            // Nautilus (GNOME) - has good select support
            ("nautilus", $"--select \"{filePath}\""),
        
            // Dolphin (KDE) - select support
            ("dolphin", $"--select \"{filePath}\""),
        
            // Nemo (Cinnamon) - select support
            ("nemo", $"\"{filePath}\""),
        
            // Try just opening the directory with various managers
            ("nautilus", $"\"{directory}\""),
            ("dolphin", $"\"{directory}\""),
            ("nemo", $"\"{directory}\""),
            ("thunar", $"\"{directory}\""),
            ("pcmanfm", $"\"{directory}\""),
            ("caja", $"\"{directory}\""),
        
            // Final fallback: xdg-open
            ("xdg-open", $"\"{directory}\"")
        };

        foreach (var (command, args) in attempts)
        {
            if (IsCommandAvailable(command))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = args,
                        UseShellExecute = true
                    });
                    return;
                }
                catch
                {
                    // Try next option
                }
            }
        }
    }

    private static bool IsCommandAvailable(string command)
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "which",
                Arguments = command,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
        
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}