using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using COMPASS.Common.Interfaces.Repos;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.Services.StateManagers
{
    public static class CollectionManager
    {
        #region Properties
    
        private static readonly ObservableCollection<CodexCollectionVM> _allCollectionVms = [];

        //Needed for binding to context menu "Move to Collection"
        public static IReadOnlyCollection<string> CollectionNames => _allCollectionVms.Select(collectionState => collectionState.Collection.Name)
                                                                                      .ToList()
                                                                                      .AsReadOnly();
        public static IReadOnlyCollection<CodexCollectionVM> CollectionVms => _allCollectionVms;
        #endregion

        #region Methods
    
        public static bool IsValidCollectionName(string? proposedName, [NotNullWhen(false)] out string? invalidReason, IList<CodexCollectionVM>? existingCollections = null)
        {
            existingCollections ??= _allCollectionVms;
            invalidReason = null;
            
            //Not empty
            if (string.IsNullOrWhiteSpace(proposedName))
            {
                invalidReason = "Collection name cannot be empty";
                return false;
            }

            //No invalid chars
            var invalidChars = Path.GetInvalidPathChars().Concat(Path.GetInvalidFileNameChars()).ToArray();
            if (proposedName.IndexOfAny(invalidChars) is var invalidCharIndex && invalidCharIndex >= 0)
            {
                invalidReason = $"Character ${invalidChars[invalidCharIndex]} is not allowed";
                return false;
            }

            //Must be unique
            if (existingCollections.Any(col => col.Identifier == proposedName))
            {
                invalidReason = $"Collection {proposedName} already exists";
                return false;
            }
            
            //Not too long
            if (proposedName.Length > 127)
            {
                invalidReason = $"Collection name is too long";
                return false;
            }
            
            //Cannot start with __, reserved
            if (proposedName.Length >= 2 && proposedName[..2] == "__")
            {
                invalidReason = $"Collection name should not start with 2 underscores";
                return false;
            }

            return true;
        }
    
        public static void DiscoverCollections()
        {
            foreach (StorageStrategy strategy in Enum.GetValues<StorageStrategy>())
            {
                var storageService = ServiceResolver.ResolveKeyed<ICodexCollectionRepository>(strategy);
                storageService.Init();
            
                var foundCollections = storageService.GetAllCollections();
                foreach (CodexCollection collection in foundCollections)
                {
                    CodexCollectionVM vm = new(collection.Name, collection, storageService);
                    RegisterCollection(vm);
                }
            }
        }

        public static void RegisterCollection(CodexCollectionVM collectionVm)
        {
            if (_allCollectionVms.Any(vm => vm.Identifier == collectionVm.Identifier))
            {
                //TODO make this more robust by finding a name that is valid and renaming the collection to it
                throw new InvalidOperationException("Collection with the same name already exists.");
            }
            
            _allCollectionVms.Add(collectionVm);
        }
    
        /// <summary>
        /// Creates a new collection
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static CodexCollectionVM CreateCollection(string identifier)
        {
            if (!IsValidCollectionName(identifier, out string? invalidReason, _allCollectionVms))
            {
                Logger.Warn(invalidReason);
                throw new InvalidOperationException(invalidReason);
            }

            CodexCollection newCollection = new(identifier);
        
            //save to xml by default
            var storageService = ServiceResolver.ResolveKeyed<ICodexCollectionRepository>(StorageStrategy.Xml);
            storageService.AllocateNewCollection(newCollection);
            var newCollectionVm = new CodexCollectionVM(newCollection.Name, newCollection, storageService);
            RegisterCollection(newCollectionVm);
            
            return newCollectionVm;
        }
    
        public static CollectionHandle GetOrCreateInitialCollectionVM()
        {
            var collectionOptions = _allCollectionVms;
            var collectionHandle = LoadInitialCollection(_allCollectionVms);

            if (collectionHandle != null) return collectionHandle;
        
            Debug.Assert(collectionOptions.Count == 0, "Collection should only be null if all options have been tried and failed");
            string name = Constants.DEFAULT_COLLECTION_NAME;

            collectionHandle = CreateAndLoadCollection(name);
            if (collectionHandle == null)
            {
                //If no collections are found and creation fails, we are stuck in an infinite loop which is bad so throw and crash
                throw new IOException($"Could not create the default collection");
            }

            return collectionHandle;
        }
    
        private static CollectionHandle? LoadInitialCollection(IList<CodexCollectionVM> availableCollections)
        {
            CollectionHandle? collectionHandle = null;
        
            string startupCollectionId = PreferencesService.GetInstance().Preferences.UIState.StartupCollection;
        
            while (collectionHandle  == null)
            {
                //no collections to load
                if (availableCollections.Count == 0)
                {
                    return null;
                }
            
                //otherwise, open startup collection
                else if (availableCollections.Any(collectionState => collectionState.Identifier == startupCollectionId))
                {
                    var startupCollection = availableCollections.First(collection => collection.Identifier == startupCollectionId);
                    collectionHandle = startupCollection.Load();
                    if (collectionHandle == null)
                    {
                        // if loading failed -> remove it from the pool and try again
                        availableCollections.Remove(startupCollection);
                    }
                }

                //in case startup collection no longer exists, pick first one that does exists
                else
                {
                    Logger.Warn($"The collection {startupCollectionId} could not be found.",
                        new DirectoryNotFoundException());
                    var firstCollection = availableCollections.First();
                    collectionHandle = firstCollection.Load();
                    if (collectionHandle == null)
                    {
                        // if loading failed -> remove it from the pool and try again
                        availableCollections.RemoveAt(0);
                    }
                }
            }
        
            return collectionHandle;
        }
    
        public static CollectionHandle? CreateAndLoadCollection(string? dirName)
        {
            if (dirName == null)
            {
                return null;
            }

            CodexCollectionVM newCollectionVm;
            try
            {
                newCollectionVm = CreateCollection(dirName);
            }
            catch (InvalidOperationException ex)
            {
                Logger.Warn("Failed to create the collection", ex);
                return null;
            }

            return newCollectionVm.Load();
        }

        public static void RemoveCollection(CodexCollectionVM collectionToDelete)
        {
            _allCollectionVms.Remove(collectionToDelete);
        }
    
        public static bool CollectionExists(string collectionIdentifier)
        {
            return _allCollectionVms.Any(c => c.Identifier == collectionIdentifier);
        }
        
        public static CodexCollectionVM? GetCollectionVM(string collectionIdentifier)
        {
            return _allCollectionVms.SingleOrDefault(c => c.Identifier == collectionIdentifier);
        }
    
        public static CollectionHandle? LoadCollection(string collectionIdentifier)
        {
            CodexCollectionVM? codexCollectionVm = _allCollectionVms.SingleOrDefault(c => c.Identifier == collectionIdentifier);
            return codexCollectionVm?.Load();
        }

        public static void SaveAllCollections()
        {
            foreach (CodexCollectionVM collectionVm in _allCollectionVms)
            {
                if (collectionVm.IsLoaded)
                {
                    collectionVm.Collection.Save();
                }
            }
        }
        
        #endregion

        #region Extension methods

        public static void Save(this CodexCollection collection)
        {
            using var collectionHandler = LoadCollection(collection.Name);
            collectionHandler?.Save();
        }
        
        public static void SaveCodices(this CodexCollection collection)
        {
            using var collectionHandler = LoadCollection(collection.Name);
            collectionHandler?.SaveCodices();
        }

        public static CollectionHandle? Load(this CodexCollection collection)
        {
            return LoadCollection(collection.Name);
        }

        #endregion
    }
}