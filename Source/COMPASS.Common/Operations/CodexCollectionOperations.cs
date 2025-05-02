using System.Collections.Generic;
using System.IO;
using System.Linq;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Services;
using COMPASS.Common.Tools;

namespace COMPASS.Common.Operations;

public static class CodexCollectionOperations
{
    public static bool IsLegalCollectionName(string? dirName, IList<CodexCollection> existingCollections)
    {
        bool legal =
            !string.IsNullOrWhiteSpace(dirName)
            && dirName.IndexOfAny(Path.GetInvalidPathChars()) < 0
            && dirName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
            && existingCollections.All(col => col.Name != dirName)
            && dirName.Length < 100
            && (dirName.Length < 2 || dirName[..2] != "__"); //reserved for protected folders
        return legal;
    }
    
    public static CodexCollection? LoadInitialCollection(IList<CodexCollection> collections)
    {
        string startupCollectionName = PreferencesService.GetInstance().Preferences.UIState.StartupCollection;
        var collectionStorageService = ServiceResolver.Resolve<ICodexCollectionStorageService>();
        
        CodexCollection? loadedCollection = null;
        while (loadedCollection  == null)
        {
            //no collections to load
            if (collections.Count == 0)
            {
                return null;
            }
            
            //otherwise, open startup collection
            else if (collections.Any(collection => collection.Name == startupCollectionName))
            {
                var startupCollection = collections.First(collection => collection.Name == startupCollectionName);
                int loadResult = collectionStorageService.Load(startupCollection);
                if (loadResult != 0)
                {
                    // if loading failed -> remove it from the pool and try again
                    collections.Remove(startupCollection);
                }
                else
                {
                    loadedCollection = startupCollection;
                }
            }

            //in case startup collection no longer exists, pick first one that does exists
            else
            {
                Logger.Warn($"The collection {startupCollectionName} could not be found.",
                    new DirectoryNotFoundException());
                var firstCollection = collections.First();
                int loadResult = collectionStorageService.Load(firstCollection);
                if (loadResult != 0)
                {
                    // if loading failed -> remove it from the pool and try again
                    collections.RemoveAt(0);
                }
                else
                {
                    loadedCollection = firstCollection;
                }
            }
        }
        
        return loadedCollection;
    }
}