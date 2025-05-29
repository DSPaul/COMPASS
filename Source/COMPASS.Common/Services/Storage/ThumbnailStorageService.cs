using System;
using System.IO;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;

namespace COMPASS.Common.Services.Storage;

public class ThumbnailStorageService : IThumbnailStorageService
{
    public ThumbnailStorageService(IEnvironmentVarsService environmentVarsService)
    {
        _collectionsPath = Path.Combine(environmentVarsService.CompassDataPath, "Collections");
    }

    private readonly string _collectionsPath;
    
    private string CoverArtDirectory(CodexCollection collection) => Path.Combine(_collectionsPath, collection.Name, "CoverArt");
    private string DefaultCoverArtPath(Codex codex) => Path.Combine(CoverArtDirectory(codex.Collection), $"{codex.ID}.png");
    
    private string ThumbnailDirectory(CodexCollection collection) => Path.Combine(_collectionsPath, collection.Name, "Thumbnails");
    private string DefaultThumbnailPath(Codex codex) => Path.Combine(ThumbnailDirectory(codex.Collection), $"{codex.ID}.png");

    private bool EnsureDirectoriesExists(CodexCollection collection)
    {
        bool coverArtDirExists = IOService.EnsureDirectoryExists(CoverArtDirectory(collection));
        bool thumbnailDirExists = IOService.EnsureDirectoryExists(ThumbnailDirectory(collection));
        
        return coverArtDirExists && thumbnailDirExists;
    }

    public void InitCodexImagePaths(Codex codex)
    {
        codex.CoverArtPath = DefaultCoverArtPath(codex);
        codex.ThumbnailPath = DefaultThumbnailPath(codex);
    }
    
    public void MoveCodexDataToCollection(Codex codex, CodexCollection targetCollection, bool copy = false)
    {
        bool dirsExist = EnsureDirectoriesExists(targetCollection);

        if (!dirsExist)
        {
            //TODO, throw error of some kind 
            return;
        }

        string newCoverPath = Path.Combine(CoverArtDirectory(targetCollection), $"{codex.ID}.png");
        string newThumbnailPath = Path.Combine(ThumbnailDirectory(targetCollection), $"{codex.ID}.png");
        
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
            Logger.Warn($"Failed to copy cover of {codex.Title}", ex);
        }

        //Move Thumbnail file
        try
        {
            if (copy)
            {
                File.Copy(codex.ThumbnailPath, newThumbnailPath, true);
            }
            else
            {
                File.Move(codex.ThumbnailPath, newThumbnailPath, true);
            }
        }
        catch (FileNotFoundException)
        {
            //File didn't exist, nothing to copy
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to copy thumbnail of {codex.Title}", ex);
        }
        
        //update img path to these new files
        codex.CoverArtPath = newCoverPath;
        codex.ThumbnailPath = newThumbnailPath;
        
        //if no thumbnail file was moved, create one
        if (!File.Exists(codex.ThumbnailPath))
        {
            CoverService.CreateThumbnail(codex);
        }
    }

    /// <summary>
    /// Change the img paths on every codex to link the the new collection location
    /// </summary>
    /// <param name="collection"></param>
    public void OnCollectionRenamed(CodexCollection collection)
    {
        foreach (Codex codex in collection.AllCodices)
        {
            //Replace folder names in image paths
            InitCodexImagePaths(codex);
        }
    }
    
    public void OnCodexDeleted(Codex codex)
    {
        //Cleanup codes files, take care not to remove user files
        try
        {
            if (codex.CoverArtPath.StartsWith(CoverArtDirectory(codex.Collection)))
            {
                File.Delete(codex.CoverArtPath);
            }
            if (codex.ThumbnailPath.StartsWith(ThumbnailDirectory(codex.Collection)))
            {
                File.Delete(codex.ThumbnailPath);
            }
        }
        catch
        {
            //deleting the thumbnail could fail because of many reasons,
            //not a big deal as it will just get overwritten when a new codex gets the freed id
        }
    }
}