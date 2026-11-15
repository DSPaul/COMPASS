using Avalonia.Platform.Storage;
using COMPASS.Common.Domain;
using COMPASS.Infra.IO;

namespace COMPASS.Common.Features.ImportExport;

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

    /// <summary>
    /// Take storage items and turn them into paths that can be imported, sorted into folders and files.
    /// </summary>
    /// <param name="storageItems"></param>
    /// <param name="collection"></param>
    /// <returns></returns>
    Task<PathClassification> GetImportablePathsAsync(IEnumerable<IStorageItem> storageItems, CodexCollection collection);

    #endregion

    #region Export

    Task ExportCollection(CodexCollection collection, IStorageFile? file, bool includeFiles, bool includeCovers);

    Task ExportTags(CodexCollection collection);

    void CompressUserDataToZip(string zipPath);

    #endregion

    FilePickerFileType SatchelExtensionFilter { get; }
}