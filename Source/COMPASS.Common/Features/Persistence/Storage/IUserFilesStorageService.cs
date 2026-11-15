using Avalonia.Platform.Storage;
using COMPASS.Common.Domain;

namespace COMPASS.Common.Features.Persistence.Storage;

public interface IUserFilesStorageService
{
    bool HasUserFiles(CodexCollection collection);

    bool EnsureDirectoryExists(CodexCollection collection);
    
    /// <summary>
    /// Copies the file referenced in path to the new collection if no source is given
    /// or if it was also in the user file path in the source collection 
    /// </summary>
    /// <param name="codex"></param>
    /// <param name="targetCollection"></param>
    /// <param name="sourceCollection"></param>
    /// <param name="copy"> Will copy rather than move the file </param>
    void MoveCodexDataToCollection(Codex codex, CodexCollection targetCollection, CodexCollection? sourceCollection = null, bool copy = false);

    /// <summary>
    /// Saves the contents of files that have no path on disk, such as an entry dragged out of a ZIP folder
    /// or a mail attachment, into the collection's file folder. Returns the paths they were saved to,
    /// leaving out the ones that could not be saved.
    /// </summary>
    Task<List<string>> SaveStorageFiles(CodexCollection collection, IEnumerable<IStorageFile> storageFiles, CancellationToken cancellationToken = default);
}