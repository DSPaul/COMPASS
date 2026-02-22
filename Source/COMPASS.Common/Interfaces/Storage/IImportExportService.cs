using Avalonia.Platform.Storage;
using COMPASS.Common.Models;

namespace COMPASS.Common.Interfaces.Storage;

public interface IImportExportService
{
    #region Import

    /// <summary>
    /// Unpack the satchel at the given location
    /// Will be extracted to the default collection path as a temp collection
    /// </summary>
    /// <param name="satchelPath"></param>
    /// <returns> The loaded collection</returns>
    Task<CodexCollection?> OpenSatchel(string? satchelPath = null);

    #endregion

    #region Export

    Task ExportCollection(CodexCollection collection, IStorageFile? file, bool includeFiles, bool includeCovers);

    Task ExportTags(CodexCollection collection);

    void CompressUserDataToZip(string zipPath);

    #endregion
}