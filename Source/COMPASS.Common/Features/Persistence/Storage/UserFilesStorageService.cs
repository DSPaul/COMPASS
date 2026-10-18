using COMPASS.Infra.IO;
using COMPASS.Infra.Logging;
using COMPASS.Common.Domain;

namespace COMPASS.Common.Features.Persistence.Storage;

public class UserFilesStorageService(
    IApplicationDataService applicationDataService,
    IIOService ioService,
    ILogger logger
    ) : IUserFilesStorageService
{
    private readonly string _collectionsPath = Path.Combine(applicationDataService.UserDataPath, Constants.DIR_COLLECTIONS);
    

    #region IUserFilesStorageService
    private string UserFilesDirectory(CodexCollection collection) => Path.Combine(_collectionsPath, collection.Name , Constants.DIR_FILES);

    public bool HasUserFiles(CodexCollection collection)
    {
        string path = UserFilesDirectory(collection);

        try
        {
            return Path.Exists(path) && Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Any();
        }
        catch (Exception ex)
        {
            //failed to read the files, cou
            logger.Warn($"Failed to check if User Files Storage folder contains any files for collection {collection.Name}", ex);
            return false;
        }
    }

    public bool EnsureDirectoryExists(CodexCollection collection)
    {
        string userFilesPath = UserFilesDirectory(collection);
        try
        {
            ioService.EnsureDirectoryExists(userFilesPath);
            return true;
        }
        catch (Exception ex)
        {
            logger.Error("Failed to create a folder to store the imported files", ex);
            return false;
        }
    }
    
    public void MoveCodexDataToCollection(Codex codex, CodexCollection targetCollection, CodexCollection? sourceCollection = null, bool copy = false)
    {
        //if no prev User files dir is given, we just use the current folder 
        if (string.IsNullOrEmpty(codex.Sources.Path))
        {
            return;
        }
        
        EnsureDirectoryExists(targetCollection);
        
        string? oldPath;
        string newPath;

        if (!Path.IsPathFullyQualified(codex.Sources.Path)) //relative path
        {
            newPath = Path.Combine(UserFilesDirectory(targetCollection), codex.Sources.Path); // keep relative structure
            if(sourceCollection == null)
            {
                oldPath = null;
                logger.Warn($"Unable to move the file references by {codex.Title} because it has a relative path");
            }
            else
            {
                oldPath = Path.Combine(UserFilesDirectory(sourceCollection), codex.Sources.Path);
            }

        }
        else //absolute path
        {
            oldPath = codex.Sources.Path;

            //if no previous collection given, put it top level in user files directory
            if (sourceCollection == null)
            {
                newPath = Path.Combine(UserFilesDirectory(targetCollection), Path.GetFileName(codex.Sources.Path));
            }
            //if existing user files dir is given, replace it with new dir
            else if (codex.Sources.Path.StartsWith(UserFilesDirectory(sourceCollection)))
            {
                newPath = codex.Sources.Path.Replace(UserFilesDirectory(sourceCollection), UserFilesDirectory(targetCollection));
            }
            //if the file wasn't in the previous user files, doesn't nee to be copied over
            else
            {
                return;
            }
        }


        if (newPath == codex.Sources.Path || oldPath == null)
        {
            return;
        }
        
        try
        {
            ioService.EnsureDirectoryExists(newPath);
        }
        catch (Exception ex)
        {
            logger.Error("Failed to create a folder to store the imported files", ex);
        }

        try
        {
            if (copy)
            {
                File.Copy(oldPath, newPath, true);
            }
            else
            {
                File.Move(oldPath, newPath, true);
            }
            codex.Sources.Path = newPath;
        }
        catch (Exception ex)
        {
            logger.Warn($"Failed to copy file associated with {codex.Title}", ex);
        }
    }
    #endregion

}