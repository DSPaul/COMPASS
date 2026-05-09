using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;

namespace COMPASS.Common.Services.Storage;

public class CoverStorageService(
    IApplicationDataService applicationDataService,
    IIOService ioService,
    ILogger logger) 
    : ICoverStorageService
{
    private readonly string _collectionsPath = Path.Combine(applicationDataService.UserDataPath, Constants.DIR_COLLECTIONS);

    private string CoverArtDirectory(CodexCollection collection) => Path.Combine(_collectionsPath, collection.Name, Constants.DIR_COVERS);
    private string DefaultCoverArtPath(Codex codex) => Path.Combine(CoverArtDirectory(codex.Collection), $"{codex.Id}.png");

    private bool EnsureDirectoriesExists(CodexCollection collection) => ioService.EnsureDirectoryExists(CoverArtDirectory(collection));

    public void InitCodexImagePaths(Codex codex)
    {
        codex.CoverArtPath = DefaultCoverArtPath(codex);
    }
    
    public void MoveCodexDataToCollection(Codex codex, CodexCollection targetCollection, bool copy = false)
    {
        bool dirsExist = EnsureDirectoriesExists(targetCollection);

        if (!dirsExist)
        {
            logger.Warn($"Failed to create cover directory for collection {targetCollection.Name}, covers will not be moved");
            return;
        }

        string newCoverPath = Path.Combine(CoverArtDirectory(targetCollection), $"{codex.Id}.png");

        //Move Cover file
        try
        {
            if (copy)
            {
                File.Copy(codex.CoverArtPath, newCoverPath, true);
            }
            else
            {
                File.Move(codex.CoverArtPath, newCoverPath, true);
            }
        }
        catch (FileNotFoundException)
        {
            //File didn't exist, nothing to copy
        }
        catch (Exception ex)
        {
            logger.Warn($"Failed to copy cover of {codex.Title}", ex);
        }

        codex.CoverArtPath = newCoverPath;

    }

    /// <summary>
    /// Updates cover art paths after a collection rename.
    /// </summary>
    /// <param name="collection"></param>
    public void OnCollectionRenamed(CodexCollection collection)
    {
        foreach (Codex codex in collection.AllCodices)
        {
            codex.CoverArtPath = DefaultCoverArtPath(codex);
        }
    }
    
    public void OnCodexDeleted(Codex codex)
    {
        //Cleanup codex files, take care not to remove user files
        try
        {
            if (codex.CoverArtPath.StartsWith(CoverArtDirectory(codex.Collection)))
            {
                File.Delete(codex.CoverArtPath);
            }

            File.Delete(codex.ThumbnailPath);
        }
        catch(Exception ex)
        {
            logger.Debug($"Failed to delete cover or thumbnail: {ex.Message}");
            //deleting the cover and thumbnail could fail because of many reasons,
            //not a big deal as it will just get overwritten in case of the cover when a new codex gets the freed id
            //or be an orphaned thumbnail in case of the thumbnail, so we can just ignore any exceptions here
        }
    }
}