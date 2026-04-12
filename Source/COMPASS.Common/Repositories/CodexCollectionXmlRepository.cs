using COMPASS.Common.Interfaces.Repos;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.XmlDtos;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.Tools;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Tools;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace COMPASS.Common.Repositories
{
    internal class CodexCollectionXmlRepository(
        IApplicationDataService applicationDataService,
        INotificationService windowedNotificationService)
         : ICodexCollectionRepository
    {
        private string _collectionsPath = Path.Combine(applicationDataService.UserDataPath, Constants.DIR_COLLECTIONS);
        private readonly Lock _codicesLocker = new();
        private readonly Lock _tagsLocker = new();
        private readonly Lock _infoLocker = new();

        private const string CodicesFileName = "CodexInfo.xml";
        private const string TagsFileName = "Tags.xml";
        private const string CollectionInfoFileName = "CollectionInfo.xml";

        private string CollectionDataPath(string collectionName) => Path.Combine(_collectionsPath, collectionName);
        private string CodicesDataFilePath(string collectionName) => Path.Combine(CollectionDataPath(collectionName), CodicesFileName);
        private string TagsDataFilePath(string collectionName) => Path.Combine(CollectionDataPath(collectionName), TagsFileName);
        private string CollectionInfoFilePath(string collectionName) => Path.Combine(CollectionDataPath(collectionName), CollectionInfoFileName);

        public void Init()
        {
            //Create the Collections directory if it does not exist
            while (!Directory.Exists(_collectionsPath))
            {
                try
                {
                    Directory.CreateDirectory(_collectionsPath);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to create folder to store user data, so data cannot be saved", ex);
                    string msg = $"Failed to create a folder to store user data at {applicationDataService.UserDataPath}, " +
                                 $"please pick a new location to save your data. Creation failed with the following error {ex.Message}";
                    applicationDataService.RequireNewUserDataLocation(msg).Wait();
                    _collectionsPath = Path.Combine(applicationDataService.UserDataPath, Constants.DIR_COLLECTIONS);
                }
            }
        }

        #region Create 
        public void AllocateNewCollection(CodexCollection collection)
        {
            collection.LoadedCodices = true;
            collection.LoadedInfo = true;
            collection.LoadedTags = true;

            CreateDirectories(collection.Name);
        }
        #endregion

        #region Read
        public IList<CodexCollection> GetAllCollections()
        {
            try
            {
                //Get all collections by folder name
                return Directory
                    .GetDirectories(_collectionsPath)
                    .Select(dir => Path.GetFileName(dir))
                    .Where(dir => CollectionManager.IsValidCollectionName(dir, out _, []))
                    .Select(dir => new CodexCollection(dir))
                    .ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to find existing collections in {_collectionsPath}", ex);
                return [];
            }
        }

        #region Load Data From File

        /// <summary>
        /// Loads the collection and unless MakeStartupCollection, sets it as the new default to load on startup
        /// </summary>
        /// <returns>int that gives status: 0 for success, -1 for failed tags, -2 for failed codices, -4 for failed info, or combination of those</returns>
        public int Load(CodexCollection collection)
        {
            int result = 0;
            bool loadedTags = LoadTags(collection);
            bool loadedCodices = LoadCodices(collection);
            bool loadedInfo = LoadInfo(collection);
            if (!loadedTags)
            {
                result -= 1;
            }

            if (!loadedCodices)
            {
                result -= 2;
            }

            if (!loadedInfo)
            {
                result -= 4;
            }

            return result;
        }

        public void Unload(CodexCollection collection)
        {
            collection.LoadedCodices = false;
            collection.LoadedInfo = false;
            collection.LoadedTags = false;
        }

        //Loads the RootTags from a file and constructs the AllTags list from it
        public bool LoadTags(CodexCollection collection)
        {
            List<TagDto> loadedTags = [];
            if (File.Exists(TagsDataFilePath(collection.Name)))
            {
                lock (_tagsLocker) //lock so file cannot change while we are reading it
                {
                    using var reader = new StreamReader(TagsDataFilePath(collection.Name));
                    XmlSerializer serializer = GetSerializer(typeof(List<TagDto>));
                    try
                    {
                        loadedTags = serializer.Deserialize(reader) as List<TagDto> ?? [];
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Could not load {TagsDataFilePath(collection.Name)}.", ex);
                        return false;
                    }
                }
            }

            collection.RootTags = loadedTags.Select(dto => dto.ToModel()).ToList();

            collection.LoadedTags = true;
            return true;
        }

        //Loads AllCodices list from Files
        public bool LoadCodices(CodexCollection collection)
        {
            //Tags should be loaded before codices
            if (!collection.LoadedTags)
            {
                LoadTags(collection);
            }

            CodexDto[] dtos = [];
            if (File.Exists(CodicesDataFilePath(collection.Name)))
            {
                lock (_codicesLocker) //lock so file cannot change while we are reading it
                {
                    using var reader = new StreamReader(CodicesDataFilePath(collection.Name));
                    XmlSerializer serializer = GetSerializer(typeof(CodexDto[]));
                    try
                    {
                        dtos = serializer.Deserialize(reader) as CodexDto[] ?? [];
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Could not load {CodicesDataFilePath(collection.Name)}", ex);
                        return false;
                    }
                }
            }

            //Must be list fist because subscribers of AllCodices collection changed will each enumerate, 
            //and can thus otherwise have different instances of the codices
            var codices = dtos.Select(dto => dto.ToModel(collection)).ToList();
            collection.AllCodices.ReplaceRange(codices);

            collection.LoadedCodices = true;
            return true;
        }

        public bool LoadInfo(CodexCollection collection)
        {
            Debug.Assert(collection.LoadedTags);

            if (File.Exists(CollectionInfoFilePath(collection.Name)))
            {
                lock (_infoLocker) //lock so file cannot change while we are reading it
                {
                    try
                    {
                        using var reader = new StreamReader(CollectionInfoFilePath(collection.Name));
                        XmlSerializer serializer = GetSerializer(typeof(CollectionInfoDto));
                        if (serializer.Deserialize(reader) is not CollectionInfoDto loadedInfo)
                        {
                            Logger.Warn($"Could not load info for {CollectionInfoFilePath(collection.Name)}");
                            return false;
                        }

                        collection.Info = loadedInfo.ToModel(collection.AllTags);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Could not load info for {CollectionInfoFilePath(collection.Name)}", ex);
                        return false;
                    }
                }
            }
            else
            {
                collection.Info = new();
            }

            collection.LoadedInfo = true;
            return true;
        }

        private XmlSerializer GetSerializer(Type type)
        {
            //Obsolete properties should still be deserialized for backwards compatibility
            var overrides = new XmlAttributeOverrides();
            var obsoleteAttributes = new XmlAttributes { XmlIgnore = false };
            var obsoleteProperties = type.GetObsoleteProperties();
            foreach (string prop in obsoleteProperties)
            {
                overrides.Add(type, prop, obsoleteAttributes);
            }

            return new(type, overrides);
        }

        #endregion
        #endregion

        #region Update
        #region Save Data To XML File

        public void CreateDirectories(string collectionName)
        {
            try
            {
                Directory.CreateDirectory(CollectionDataPath(collectionName));
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to create folder to store user data for this collection.", ex);

                string msg = $"Failed to create the necessary folders to store data about this collection. The following error occured";
                Notification failedFolderCreation = new("Failed to save collection", msg, Severity.Error)
                {
                    Details = ex.ToString()
                };
                windowedNotificationService.Notify(failedFolderCreation);
            }
        }

        public bool Save(CodexCollection collection)
        {
            try
            {
                Directory.CreateDirectory(CollectionDataPath(collection.Name));
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to create the folder to save the data for this collection", ex);
                return false;
            }

            bool savedTags = SaveTags(collection);
            bool savedCodices = SaveCodices(collection);
            bool savedInfo = SaveInfo(collection);

            if (savedCodices || savedTags || savedInfo)
            {
                Logger.Info($"Saved {collection.Name}");
                return true;
            }

            return false;
        }

        public bool SaveTags(CodexCollection collection, Stream? stream = null)
        {
            if (stream == null && !collection.LoadedTags)
            {
                //Should always load a collection before it can be saved to disk to avoid data loss
                return false;
            }

            var toSave = collection.RootTags.Select(c => c.ToDto()).ToList();
            return WriteXml(stream, TagsDataFilePath(collection.Name), toSave, typeof(List<TagDto>), _tagsLocker, "Tags");
        }

        public bool SaveCodices(CodexCollection collection, Stream? stream = null)
        {
            if (stream == null && !collection.LoadedCodices)
            {
                //Should always load a collection before it can be saved to disk to avoid data loss
                return false;
            }

            var toSave = collection.AllCodices.Select(c => c.ToDto()).ToList();
            return WriteXml(stream, CodicesDataFilePath(collection.Name), toSave, typeof(List<CodexDto>), _codicesLocker, "Codices");
        }

        public bool SaveInfo(CodexCollection collection, Stream? stream = null)
        {
            if (stream == null && !collection.LoadedInfo)
            {
                //Should always load a collection before it can be saved to disk to avoid data loss
                return false;
            }

           return WriteXml(stream, CollectionInfoFilePath(collection.Name), collection.Info.ToDto(), typeof(CollectionInfoDto), _infoLocker, "Collection Info");
        }

        private static bool WriteXml(Stream? stream, string targetPath, object toSave, Type type, Lock @lock, string objectName)
        {
            try
            {
                string tempFileName = targetPath + ".tmp";

                lock (@lock)
                {
                    using (var writer = stream != null
                        ? XmlWriter.Create(stream, XmlService.XmlWriteSettings)
                        : XmlWriter.Create(tempFileName, XmlService.XmlWriteSettings))
                    {
                        XmlSerializer serializer = new(type);
                        serializer.Serialize(writer, toSave);
                    }

                    if(stream != null)
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                    }
                    else if (Path.Exists(tempFileName))
                    {
                        //if successfully written to the tmp file, move to actual path
                        File.Move(tempFileName, targetPath, true);
                        File.Delete(tempFileName);
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Error($"Access denied when trying to save {objectName} to {targetPath}", ex);
                return false;
            }
            catch (IOException ex)
            {
                Logger.Error($"IO error occurred when saving {objectName} to {targetPath}", ex);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to save {objectName} to {targetPath}", ex);
                return false;
            }

            return true;
        }


        #endregion

        /// <summary>
        /// Rename the directory to match the new collection name
        /// </summary>
        /// <param name="oldName"></param>
        /// <param name="newName"></param>
        public void OnCollectionRenamed(string oldName, string newName)
        {
            try
            {
                Directory.Move(Path.Combine(_collectionsPath, oldName), Path.Combine(_collectionsPath, newName));
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to move data files from {oldName} to {newName}", ex);
            }
        }

        #endregion

        #region Delete

        public void DeleteCollection(string collectionId)
        {
            //if Dir name of toDelete is empty, it will delete the entire collections folder
            if (string.IsNullOrEmpty(collectionId))
                return;

            //nothing to delete if the collection was never saved
            if (!Directory.Exists(CollectionDataPath(collectionId)))
                return;

            try
            {
                //sometimes completing delete fails because a files are locked, retry could help with that
                Utils.Retry<IOException>(3, () => Directory.Delete(CollectionDataPath(collectionId), true),
                    onFailedAttempt: (ex) => Logger.Warn($"Failed to delete collection {collectionId}, retrying...", ex)
                );
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to delete collection {collectionId}", ex);
            }
        }

        #endregion
    }
}
