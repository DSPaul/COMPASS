using COMPASS.Infra.Models.Progress;

namespace COMPASS.Common.Interfaces.Services;

public interface IIOService
{
    #region Tmp data

    void ClearTmpData(string? tempPath = null);

    #endregion

    #region Dialogs/explorer

    void ShowInExplorer(string filePath);

    /// <summary>
    /// Allow the user to select a single folder using a dialog
    /// </summary>
    /// <returns> the selected path, null if canceled/failed / whatever </returns>
    Task<string?> PickFolder();

    /// <summary>
    /// Allow the user to select multiple folders using a dialog
    /// </summary>
    /// <returns> IList with selected paths, empty list if canceled/failed / whatever </returns>
    Task<IList<string>> TryPickFolders();

    #endregion

    #region Manipulate data on disk

    /// <summary>
    /// Copies all files from <paramref name="sourceDir"/> to <paramref name="destDir"/>
    /// </summary>
    /// <param name="sourceDir"></param>
    /// <param name="destDir"></param>
    /// <param name="progressTracker"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>True if the copy was successful, false if canceled or an error occurred</returns>
    Task<bool> CopyDataAsync(string sourceDir, string destDir, IProgress<IProgressReport>? progressTracker = null, CancellationToken cancellationToken = default);

    #endregion

    /// <summary>
    /// Tries to create the required directories for the given path
    /// </summary>
    /// <param name="path"></param>
    bool EnsureDirectoryExists(string path);

    /// <summary>
    /// Safe alternative of <see cref="Directory.GetFiles(string)"/> that catches all exceptions
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    IEnumerable<string> TryGetFilesInFolder(string path);

    /// <summary>
    /// Safe alternative of <see cref="Directory.GetDirectories(string)"/> that catches all exceptions
    /// </summary>
    /// <param name="directory"></param>
    /// <returns></returns>
    IEnumerable<string> TryGetDirectories(string directory);
}