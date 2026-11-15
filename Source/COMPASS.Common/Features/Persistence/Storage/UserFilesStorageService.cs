using Avalonia.Platform.Storage;
using COMPASS.Infra.IO;
using COMPASS.Infra.Logging;
using COMPASS.Common.Domain;
using COMPASS.Infra.Text;

namespace COMPASS.Common.Features.Persistence.Storage;

public class UserFilesStorageService(
    IApplicationDataService applicationDataService,
    IIOService ioService,
    ILogger logger
    ) : IUserFilesStorageService
{
    private readonly string _collectionsPath = Path.Combine(applicationDataService.UserDataPath, Constants.DIR_COLLECTIONS);
    

    #region IUserFilesStorageService
    private string UserFilesDirectory(CodexCollection collection) => Path.Combine(_collectionsPath, collection.Name, Constants.DIR_FILES);

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
            Directory.CreateDirectory(userFilesPath);
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
            //relative paths are stored with portable separators, so convert to this platform's
            string relativePath = PathUtils.FromPortablePath(codex.Sources.Path);
            newPath = Path.Combine(UserFilesDirectory(targetCollection), relativePath); // keep relative structure
            if(sourceCollection == null)
            {
                oldPath = null;
                logger.Warn($"Unable to move the file references by {codex.Title} because it has a relative path");
            }
            else
            {
                oldPath = Path.Combine(UserFilesDirectory(sourceCollection), relativePath);
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

    public async Task<List<string>> SaveStorageFiles(CodexCollection collection, IEnumerable<IStorageFile> storageFiles, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(storageFiles);

        List<string> savedPaths = [];

        if (!EnsureDirectoryExists(collection))
        {
            return savedPaths;
        }

        string filesDirectory = UserFilesDirectory(collection);

        foreach (IStorageFile storageFile in storageFiles)
        {
            string fileName = Path.GetFileName(storageFile.Name);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                logger.Warn($"Skipped a file with an unusable file name: \"{storageFile.Name}\"");
                continue;
            }

            string targetPath = GetAvailablePath(filesDirectory, fileName);
            string partialPath = targetPath + PartialFileExtension;

            try
            {
                await ReadIntoFileAsync(storageFile, partialPath, cancellationToken);

                //Only a complete copy takes the place of what is already there: a read that fails
                //halfway must not leave a truncated version of the item's own file behind
                File.Move(partialPath, targetPath, overwrite: true);
                savedPaths.Add(targetPath);
            }
            catch (OperationCanceledException)
            {
                DeleteIfItExists(partialPath);
                throw;
            }
            catch (Exception exception)
            {
                logger.Warn($"Failed to save a copy of {fileName}", exception);
                DeleteIfItExists(partialPath);
            }
        }

        return savedPaths;
    }
    #endregion

    private const string PartialFileExtension = ".partial";

    private static async Task ReadIntoFileAsync(IStorageFile storageFile, string path, CancellationToken cancellationToken)
    {
        //A virtual file is read through the OLE data object of the drop, which belongs to the UI thread.
        //Its stream only implements a synchronous Read, so CopyToAsync/ReadAsync would push every read onto
        //a pool thread (RPC_E_WRONG_THREAD). Read synchronously here and only write asynchronously: without
        //ConfigureAwait(false) every write resumes on the UI thread, so the next read happens there as well.
        await using Stream contents = await storageFile.OpenReadAsync();
        await using FileStream target = new(path, FileMode.Create, FileAccess.Write, FileShare.None, CopyBufferSize, useAsync: true);

        byte[] buffer = new byte[CopyBufferSize];
        int bytesRead;
        while ((bytesRead = contents.Read(buffer, 0, buffer.Length)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await target.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }
    }

    private const int CopyBufferSize = 81920;

    private void DeleteIfItExists(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception)
        {
            logger.Warn($"Failed to clean up {path}", exception);
        }
    }

    /// <summary>
    /// The path to save the file to: its own name when that is free or already its own file, otherwise
    /// a numbered variant, so a second item with the same name does not overwrite the first.
    /// </summary>
    private static string GetAvailablePath(string filesDirectory, string fileName)
    {
        string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);

        string basePath = Path.Combine(filesDirectory, nameWithoutExtension);

        return basePath.GetValidVariant(p => !File.Exists(p + extension)) + extension;
    }
}