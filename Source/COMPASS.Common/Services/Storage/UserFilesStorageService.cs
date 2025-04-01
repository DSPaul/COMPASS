using System;
using System.IO;
using System.Linq;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;

namespace COMPASS.Common.Services.Storage;

public class UserFilesStorageService : IUserFilesStorageService
{
    public UserFilesStorageService(IEnvironmentVarsService environmentVarsService)
    {
        _collectionsPath = Path.Combine(environmentVarsService.CompassDataPath, "Collections");
    }
    
    private readonly string _collectionsPath;
    

    #region IUserFilesStorageService
    private string UserFilesDirectory(CodexCollection collection) => Path.Combine(_collectionsPath, collection.Name , "Files");

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
            Logger.Warn($"Failed to check if User Files Storage folder contains any files for collection {collection.Name}", ex);
            return false;
        }
    }

    public bool EnsureDirectoryExists(CodexCollection collection)
    {
        string userFilesPath = UserFilesDirectory(collection);
        try
        {
            IOService.EnsureDirectoryExists(userFilesPath);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to create a folder to store the imported files", ex);
            return false;
        }
    }
    
    public void MoveCodexDataToCollection(Codex codex, CodexCollection targetCollection, CodexCollection? sourceCollection = null, bool copy = false)
    {
        //if no prev User files dir is given, we just use the current folder 
        if (!File.Exists(codex.Sources.Path))
        {
            return;
        }
        
        EnsureDirectoryExists(targetCollection);
        
        string newPath;
        //if no previous collection given, put it top level in user files directory
        if (sourceCollection == null)
        {
            newPath = Path.IsPathFullyQualified(codex.Sources.Path)
                ? Path.Combine(UserFilesDirectory(targetCollection), Path.GetFileName(codex.Sources.Path)) //absolute path, just move the file top level
                : Path.Combine(UserFilesDirectory(targetCollection), codex.Sources.Path); //relative path, keep relative structure
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

        if (newPath == codex.Sources.Path)
        {
            return;
        }
        
        try
        {
            string? newDir = Path.GetDirectoryName(newPath);
            if (newDir != null)
            {
                try
                {
                    IOService.EnsureDirectoryExists(newDir);
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to create a folder to store the imported files", ex);
                }
            }

            if (copy)
            {
                File.Copy(codex.Sources.Path, newPath, true);
            }
            else
            {
                File.Move(codex.Sources.Path, newPath, true);
            }
            codex.Sources.Path = newPath;
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to copy file associated with {codex.Title}", ex);
        }
    }
    #endregion

}