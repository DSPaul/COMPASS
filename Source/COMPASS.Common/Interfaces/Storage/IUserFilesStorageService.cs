using COMPASS.Common.Models;

namespace COMPASS.Common.Interfaces.Storage;

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
}