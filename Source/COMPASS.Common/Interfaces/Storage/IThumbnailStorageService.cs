using COMPASS.Common.Models;

namespace COMPASS.Common.Interfaces.Storage;

public interface IThumbnailStorageService
{
    void InitCodexImagePaths(Codex codex);
    
    /// <summary>
    /// Copies the thumbnail and full res cover to the new collection
    /// </summary>
    /// <param name="codex"></param>
    /// <param name="targetCollection"></param>
    /// <param name="copy"> perform a copy rather than a copy </param>
    void MoveCodexDataToCollection(Codex codex, CodexCollection targetCollection, bool copy = false);
    
    void OnCollectionRenamed(CodexCollection collection);

    void OnCodexDeleted(Codex codex);
}