using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Infra.IO;
using COMPASS.Infra.Logging;

namespace COMPASS.Common.Services.Storage;

public class CoverStorageService(
    IApplicationDataService applicationDataService,
    IIOService ioService,
    ILogger logger) 
    : ICoverStorageService
{
    private readonly string _collectionsPath = Path.Combine(applicationDataService.UserDataPath, Constants.DIR_COLLECTIONS);

    public string GetCoverPath(CodexCollection collection, Codex codex) => Path.Combine(GetCoverArtDirectory(collection), $"{codex.Id}.png");
    public string GetThumbnailPath(Codex codex) => Path.Combine(GetThumbnailDirectory(), $"{codex.GlobalId}.png");

    public void MoveCodexDataToCollection(Codex codex, CodexCollection targetCollection, bool copy = false)
    {
        bool dirsExist = EnsureDirectoriesExists(targetCollection);

        if (!dirsExist)
        {
            logger.Warn($"Failed to create cover directory for collection {targetCollection.Name}, covers will not be moved");
            return;
        }

        string newCoverPath = GetCoverPath(targetCollection, codex);

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
        catch (DirectoryNotFoundException)
        {
            //Source directory didn't exist (e.g. satchel exported without covers), nothing to copy
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
            codex.CoverArtPath = GetCoverPath(collection, codex);
        }
    }
    
    public void OnCodexDeleted(Codex codex)
    {
        //Cleanup codex files, take care not to remove user files
        try
        {
            if (codex.CoverArtPath.StartsWith(GetCoverArtDirectory(codex.Collection)))
            {
                File.Delete(codex.CoverArtPath);
            }

            var thumbnailPath = GetThumbnailPath(codex);
            File.Delete(thumbnailPath);
        }
        catch(Exception ex)
        {
            logger.Debug($"Failed to delete cover or thumbnail: {ex.Message}");
            //deleting the cover and thumbnail could fail because of many reasons,
            //not a big deal as it will just get overwritten in case of the cover when a new codex gets the freed id
            //or be an orphaned thumbnail in case of the thumbnail, so we can just ignore any exceptions here
        }
    }

    private string GetCoverArtDirectory(CodexCollection collection) => Path.Combine(_collectionsPath, collection.Name, Constants.DIR_COVERS);
    private string GetThumbnailDirectory() => Path.Combine(IApplicationDataService.ApplicationDataPath, Constants.DIR_THUMBNAILS);

    private bool EnsureDirectoriesExists(CodexCollection collection)
    {
        bool coverDirExists = ioService.EnsureDirectoryExists(GetCoverArtDirectory(collection));
        bool thumbnailDirExists = ioService.EnsureDirectoryExists(GetThumbnailDirectory());
        return coverDirExists && thumbnailDirExists;
    }
}