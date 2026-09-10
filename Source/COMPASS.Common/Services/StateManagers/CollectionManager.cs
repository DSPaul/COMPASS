using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Autofac.Features.Indexed;
using COMPASS.Common.Interfaces.Repos;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.Services.StateManagers
{
    public class CollectionManager(
        ILogger logger,
        IPreferencesService preferencesService,
        CodexCollectionVMFactory codexCollectionVMFactory,
        IIndex<StorageStrategy, ICodexCollectionRepository> repositories)
    {
        #region Properties
    
        private readonly ObservableCollection<CodexCollectionVM> _allCollectionVms = [];

        //Needed for binding to context menu "Move to Collection"
        public IReadOnlyCollection<string> CollectionNames => _allCollectionVms.Select(collectionState => collectionState.Collection.Name)
                                                                                       .ToList()
                                                                                       .AsReadOnly();
        public IReadOnlyCollection<CodexCollectionVM> CollectionVms => _allCollectionVms;
        #endregion

        #region Methods
    
        public static bool IsValidCollectionName(string? proposedName, [NotNullWhen(false)] out string? invalidReason, IList<CodexCollectionVM> existingCollections)
        {
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
    
        public void DiscoverCollections()
        {
            foreach (StorageStrategy strategy in Enum.GetValues<StorageStrategy>())
            {
                var storageService = repositories[strategy];
                storageService.Init();
            
                var foundCollections = storageService.GetAllCollections();
                foreach (CodexCollection collection in foundCollections)
                {
                    CodexCollectionVM vm = codexCollectionVMFactory.Create(collection, storageService);
                    RegisterCollection(vm);
                }
            }
        }

        public void RegisterCollection(CodexCollectionVM collectionVm)
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
        public CodexCollectionVM CreateCollection(string identifier)
        {
            if (!IsValidCollectionName(identifier, out string? invalidReason, _allCollectionVms))
            {
                logger.Warn(invalidReason);
                throw new InvalidOperationException(invalidReason);
            }

            CodexCollection newCollection = new(identifier);
        
            //save to xml by default
            var storageService = repositories[StorageStrategy.Xml];

            storageService.AllocateNewCollection(newCollection);
            var newCollectionVm = codexCollectionVMFactory.Create(newCollection, storageService);
            RegisterCollection(newCollectionVm);
            
            return newCollectionVm;
        }
    
        public CollectionHandle GetOrCreateInitialCollectionVM()
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
    
        private CollectionHandle? LoadInitialCollection(IList<CodexCollectionVM> availableCollections)
        {
            CollectionHandle? collectionHandle = null;
        
            string startupCollectionId = preferencesService.Preferences.UIState.StartupCollection;
        
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
                    logger.Warn($"The collection {startupCollectionId} could not be found.",
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
    
        public CollectionHandle? CreateAndLoadCollection(string? dirName)
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
                logger.Warn("Failed to create the collection", ex);
                return null;
            }

            return newCollectionVm.Load();
        }

        public void RemoveCollection(CodexCollectionVM collectionToDelete)
        {
            _allCollectionVms.Remove(collectionToDelete);
        }
    
        public bool CollectionExists(string collectionIdentifier)
        {
            return _allCollectionVms.Any(c => c.Identifier == collectionIdentifier);
        }
        
        public CodexCollectionVM? GetCollectionVM(string collectionIdentifier)
        {
            return _allCollectionVms.SingleOrDefault(c => c.Identifier == collectionIdentifier);
        }
    
        public CollectionHandle? LoadCollection(string collectionIdentifier)
        {
            CodexCollectionVM? codexCollectionVm = _allCollectionVms.SingleOrDefault(c => c.Identifier == collectionIdentifier);
            return codexCollectionVm?.Load();
        }

        public void SaveAllCollections()
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
    }

    /// <summary>
    /// Sugar over <see cref="CollectionManager"/> for call sites that only hold the model.
    /// Single documented choke point: exactly one lazy resolve instead of dozens spread out.
    /// Prefer injecting <see cref="CollectionManager"/> in DI-built classes.
    /// Also backs XAML x:Static bindings, which cannot use DI.
    /// </summary>
    public static class CodexCollectionExtensions
    {
        private static CollectionManager Manager => field ??= ServiceResolver.Resolve<CollectionManager>();

        //Needed for binding to context menu "Move to Collection" (x:Static requires a static member)
        public static IReadOnlyCollection<string> CollectionNames => Manager.CollectionVms.Select(collectionVm => collectionVm.Collection.Name)
                                                                                          .ToList()
                                                                                          .AsReadOnly();

        public static void Save(this CodexCollection collection)
        {
            using var collectionHandler = Manager.LoadCollection(collection.Name);
            collectionHandler?.Save();
        }
        
        public static void SaveCodices(this CodexCollection collection)
        {
            using var collectionHandler = Manager.LoadCollection(collection.Name);
            collectionHandler?.SaveCodices();
        }

        public static CollectionHandle? Load(this CodexCollection collection)
        {
            return Manager.LoadCollection(collection.Name);
        }
    }
}
