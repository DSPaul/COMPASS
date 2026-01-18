using Avalonia.Platform.Storage;
using COMPASS.Common.Models;

namespace COMPASS.Common.Interfaces.Storage;

public interface IImportExportService
{
    #region Import

    /// <summary>
    /// Unpack the satchel at the given location
    /// </summary>
    /// <param name="satchelPath"></param>
    /// <returns>The collection id of the extracted collection</returns>
    Task<string?> OpenSatchel(string? satchelPath = null);

    #endregion

    #region Export

    Task ExportCollection(CodexCollection collection, IStorageFile? file, bool includeFiles, bool includeCovers);

    Task ExportTags(CodexCollection collection);

    void CompressUserDataToZip(string zipPath);

    #endregion
}