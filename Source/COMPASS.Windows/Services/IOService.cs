using System.Diagnostics;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Services.FileSystem;

namespace COMPASS.Windows.Services;

public class IOService(IFilesService filesService) : IOServiceBase(filesService)
{
    public override void ShowInExplorer(string filePath)
    {
        if (!Path.Exists(filePath))
        {
            throw new FileNotFoundException("File not found", filePath);
        }

        ProcessStartInfo startInfo = new()
        {
            Arguments = $"/select,\"{filePath}\"",
            FileName = "explorer.exe"
        };
        Process.Start(startInfo);
    }
}