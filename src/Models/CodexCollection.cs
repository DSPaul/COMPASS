using Autofac;
using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Interfaces;
using COMPASS.Models.Enums;
using COMPASS.Models.XmlDtos;
using COMPASS.Services;
using COMPASS.Tools;
using COMPASS.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace COMPASS.Models
{
    public class CodexCollection : ObservableObject
    {
        public CodexCollection(string collectionDirectory)
        {
            _directoryName = collectionDirectory;
            _preferencesService = PreferencesService.GetInstance();
        }

        private PreferencesService _preferencesService;
        private static readonly object writeLocker = new();

        public static string CollectionsPath => Path.Combine(SettingsViewModel.CompassDataPath, "Collections");
        public string FullDataPath => Path.Combine(CollectionsPath, DirectoryName);
        public string CodicesDataFilePath => Path.Combine(FullDataPath, Constants.CodicesFileName);
        public string TagsDataFilePath => Path.Combine(FullDataPath, Constants.TagsFileName);
        public string CollectionInfoFilePath => Path.Combine(FullDataPath, Constants.CollectionInfoFileName);

        public string CoverArtPath => Path.Combine(FullDataPath, "CoverArt");
        public string ThumbnailsPath => Path.Combine(FullDataPath, "Thumbnails");
        public string UserFilesPath => Path.Combine(FullDataPath, "Files");

        #region Properties
        private string _directoryName;
        public string DirectoryName
        {
            get => _directoryName;
            set => SetProperty(ref _directoryName, value);
        }

        public List<Tag> AllTags { get; private set; } = new();

        private List<Tag> _rootTags = new();
        public List<Tag> RootTags
        {
            get => _rootTags;
            set
            {
                _rootTags = value;
                foreach (Tag t in _rootTags)
                {
                    t.Parent = null;
                }
            }
        }

        private ObservableCollection<Codex> _allCodices = new();
        public ObservableCollection<Codex> AllCodices
        {
            get => _allCodices;
            private set => SetProperty(ref _allCodices, value);
        }

        public CollectionInfo Info { get; private set; } = new();

        #endregion

        #region Load Data From File

        //To prevent saving a collection that hasn't loaded yet, which would wipe all your data
        private bool _loadedTags = false;
        private bool _loadedCodices = false;
        private bool _loadedInfo = false;

        /// <summary>
        /// Loads the collection and unless MakeStartupCollection, sets it as the new default to load on startup
        /// </summary>
        /// <returns>int that gives status: 0 for success, -1 for failed tags, -2 for failed codices, -4 for failed info, or combination of those</returns>
        public int Load(bool MakeStartupCollection = true)
        {
            int result = 0;
            bool loadedTags = LoadTags();
            bool loadedCodices = LoadCodices();
            bool loadedInfo = LoadInfo();
            if (!loadedTags) { result -= 1; }
            if (!loadedCodices) { result -= 2; }
            if (!loadedInfo) { result -= 4; }
            if (MakeStartupCollection)
            {
                _preferencesService.Preferences.UIState.StartupCollection = DirectoryName;
                Logger.Info($"Loaded {DirectoryName}");
            }
            return result;
        }

        //Loads the RootTags from a file and constructs the AllTags list from it
        public bool LoadTags()
        {
            if (File.Exists(TagsDataFilePath))
            {
                //loading root tags          
                using var reader = new StreamReader(TagsDataFilePath);
                XmlSerializer serializer = new(typeof(List<Tag>));
                try
                {
                    RootTags = serializer.Deserialize(reader) as List<Tag> ?? new();
                }
                catch (Exception ex)
                {
                    Logger.Error($"Could not load {TagsDataFilePath}.", ex);
                    return false;
                }
            }
            else
            {
                RootTags = new();
            }

            CompleteLoadingTags();
            _loadedTags = true;
            return true;
        }

        private void CompleteLoadingTags()
        {
            //Constructing AllTags and pass it to all the tags
            AllTags = RootTags.Flatten().ToList();
            foreach (Tag t in AllTags)
            {
                t.AllTags = AllTags;
            }
        }

        //Loads AllCodices list from Files
        public bool LoadCodices()
        {
            CodexDto[] dtos = Array.Empty<CodexDto>();
            if (File.Exists(CodicesDataFilePath))
            {
                using var reader = new StreamReader(CodicesDataFilePath);
                XmlSerializer serializer = new(typeof(CodexDto[]));
                try
                {
                    dtos = serializer.Deserialize(reader) as CodexDto[] ?? Array.Empty<CodexDto>();
                }
                catch (Exception ex)
                {
                    Logger.Error($"Could not load {CodicesDataFilePath}", ex);
                    return false;
                }
            }

            AllCodices = new(dtos.Select(dto => dto.ToModel(AllTags)));
            _loadedCodices = true;
            return true;
        }

        public bool LoadInfo()
        {
            if (File.Exists(CollectionInfoFilePath))
            {
                try
                {
                    using var reader = new StreamReader(CollectionInfoFilePath);

                    //Obsolete properties should still be deserialized for backwards compatibility
                    var overrides = new XmlAttributeOverrides();
                    var obsoleteAttributes = new XmlAttributes { XmlIgnore = false };
                    var obsoleteProperties = Reflection.GetObsoleteProperties(typeof(CollectionInfo));
                    foreach (string prop in obsoleteProperties)
                    {
                        overrides.Add(typeof(CollectionInfo), prop, obsoleteAttributes);
                    }

                    XmlSerializer serializer = new(typeof(CollectionInfo), overrides);
                    if (serializer.Deserialize(reader) is not CollectionInfo loadedInfo)
                    {
                        Logger.Warn($"Could not load info for {CollectionInfoFilePath}");
                        return false;
                    }
                    Info = loadedInfo;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Could not load info for {CollectionInfoFilePath}", ex);
                    return false;
                }
            }
            else
            {
                Info = new();
            }

            Info.CompleteLoading(this);
            _loadedInfo = true;
            return true;
        }
        #endregion

        #region Save Data To XML File

        /// <summary>
        /// Will initialize the collection for the first time. 
        /// </summary>
        public void InitAsNew()
        {
            _loadedCodices = true;
            _loadedInfo = true;
            _loadedTags = true;

            CreateDirectories();
        }

        public void Unload()
        {
            _loadedCodices = false;
            _loadedInfo = false;
            _loadedTags = false;
        }

        public void CreateDirectories()
        {
            try
            {
                //top folder
                Directory.CreateDirectory(FullDataPath);
                //subfolders
                Directory.CreateDirectory(CoverArtPath);
                Directory.CreateDirectory(ThumbnailsPath);
                Directory.CreateDirectory(UserFilesPath);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to create folders to store user data for this collection.", ex);
                var windowedNotificationService = App.Container.ResolveKeyed<INotificationService>(NotificationDisplayType.Windowed);

                string msg = $"Failed to create the necessary folders to store data about this collection. The following error occured: {ex.Message}";
                Notification failedFolderCreation = new("Failed to save collection", msg, Severity.Error);
                windowedNotificationService.Show(failedFolderCreation);
            }
        }

        public void Save()
        {
            OnPropertyChanged(nameof(AllCodices));

            try
            {
                Directory.CreateDirectory(UserFilesPath);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to create the folder to save the data for this collection", ex);
            }

            bool savedTags = SaveTags();
            bool savedCodices = SaveCodices();
            bool savedInfo = SaveInfo();
            _preferencesService.SavePreferences();

            if (savedCodices || savedTags || savedInfo)
            {
                Logger.Info($"Saved {DirectoryName}");
            }
        }

        public bool SaveTags()
        {
            if (!_loadedTags)
            {
                //Should always load a collection before it can be saved
                return false;
            }

            try
            {
                string tempFileName = TagsDataFilePath + ".tmp";

                lock (writeLocker)
                {
                    using (var writer = XmlWriter.Create(tempFileName, XmlService.XmlWriteSettings))
                    {
                        XmlSerializer serializer = new(typeof(List<Tag>));
                        serializer.Serialize(writer, RootTags);
                    }

                    //if successfully written to the tmp file, move to actual path
                    File.Move(tempFileName, TagsDataFilePath, true);
                    File.Delete(tempFileName);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Error($"Access denied when trying to save Tags to {TagsDataFilePath}", ex);
                return false;
            }
            catch (IOException ex)
            {
                Logger.Error($"IO error occurred when saving Tags to {TagsDataFilePath}", ex);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to save Tags to {TagsDataFilePath}", ex);
                return false;
            }
            return true;
        }

        public bool SaveCodices()
        {
            if (!_loadedCodices)
            {
                //Should always load a collection before it can be saved
                return false;
            }

            var toSave = AllCodices.Select(c => c.ToDto()).ToList();

            try
            {
                string tempFileName = CodicesDataFilePath + ".tmp";

                lock (writeLocker)
                {
                    using (var writer = XmlWriter.Create(tempFileName, XmlService.XmlWriteSettings))
                    {
                        XmlSerializer serializer = new(typeof(List<CodexDto>));
                        serializer.Serialize(writer, toSave);
                    }

                    //if successfully written to the tmp file, move to actual path
                    File.Move(tempFileName, CodicesDataFilePath, true);
                    File.Delete(tempFileName);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Error($"Access denied when trying to save Codex Info to {CodicesDataFilePath}", ex);
                return false;
            }
            catch (IOException ex)
            {
                Logger.Error($"IO error occurred when saving Codex Info to {CodicesDataFilePath}", ex);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to save Codex Info to {CodicesDataFilePath}", ex);
                return false;
            }
            return true;
        }

        public bool SaveInfo()
        {
            if (!_loadedInfo)
            {
                //Should always load a collection before it can be saved
                return false;
            }
            Info.PrepareSave();
            try
            {
                string tempFileName = CollectionInfoFilePath + ".tmp";

                lock (writeLocker)
                {
                    using (var writer = XmlWriter.Create(tempFileName, XmlService.XmlWriteSettings))
                    {
                        XmlSerializer serializer = new(typeof(CollectionInfo));
                        serializer.Serialize(writer, Info);
                    }

                    //if successfully written to the tmp file, move to actual path
                    File.Move(tempFileName, CollectionInfoFilePath, true);
                    File.Delete(tempFileName);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Error($"Access denied when trying to save Collection Info to {CollectionInfoFilePath}", ex);
                return false;
            }
            catch (IOException ex)
            {
                Logger.Error($"IO error occurred when saving Collection Info to {CollectionInfoFilePath}", ex);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to save Collection Info to {CollectionInfoFilePath}", ex);
                return false;
            }
            return true;
        }

        #endregion    

        /// <summary>
        /// Will merge all the data from toMergeFrom into this collection
        /// </summary>
        /// <param name="toMergeFrom"></param>
        /// <param name="separateTags"> if true, all new tags will be put under a group with the name of the collection they came from</param>
        public async Task MergeWith(CodexCollection toMergeFrom, bool separateTags = false)
        {
            //Merge Tags
            if (separateTags)
            {
                var rootTag = new Tag(toMergeFrom.AllTags)
                {
                    IsGroup = true,
                    Content = toMergeFrom.DirectoryName.Trim('_'),
                    Children = new(toMergeFrom.RootTags)
                };
                toMergeFrom.RootTags = new() { rootTag };
            }
            AddTags(toMergeFrom.RootTags);

            //merge codices
            ImportCodicesFrom(toMergeFrom);

            //merge info
            Info.MergeWith(toMergeFrom.Info);

            //save
            if (MainViewModel.CollectionVM.CurrentCollection == this)
            {
                await MainViewModel.CollectionVM.Refresh();
            }
            else
            {
                Save();
            }
        }

        public void RenameCollection(string newCollectionName)
        {
            string oldName = DirectoryName;
            DirectoryName = newCollectionName;

            foreach (Codex codex in AllCodices)
            {
                //Replace folder names in image paths
                codex.SetImagePaths(this);
            }
            try
            {
                Directory.Move(Path.Combine(CollectionsPath, oldName), Path.Combine(CollectionsPath, newCollectionName));
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to move data files from {oldName} to {newCollectionName}", ex);
            }

            Logger.Info($"Renamed  {oldName} to {newCollectionName}");
        }

        public void AddTags(IEnumerable<Tag> tags)
        {
            // change ID's of Tags so there aren't any duplicates
            List<Tag> tagsList = tags.ToList();
            var tagsToImport = tagsList.Flatten();
            foreach (Tag tag in tagsToImport)
            {
                tag.ID = Utils.GetAvailableID(AllTags);
                tag.AllTags = AllTags;
                AllTags.Add(tag);
            }
            RootTags.AddRange(tagsList);
            MainViewModel.CollectionVM.TagsVM.BuildTagTreeView();
        }

        public void ImportCodicesFrom(CodexCollection source)
        {
            //if import includes files, make sure directory exists to copy files into
            if (Path.Exists(source.UserFilesPath))
            {
                try
                {
                    Directory.CreateDirectory(UserFilesPath);
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to create a folder to store the imported files", ex);
                }
            }

            foreach (var codex in source.AllCodices)
            {
                //Give it a new id that is unique to this collection
                codex.ID = Utils.GetAvailableID(AllCodices);

                //Move Cover file
                if (File.Exists(codex.CoverArt))
                {
                    try
                    {
                        File.Copy(codex.CoverArt, Path.Combine(CoverArtPath, $"{codex.ID}.png"), true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"Failed to copy cover of {codex.Title}", ex);
                    }
                }

                //Move Thumbnail file
                if (File.Exists(codex.Thumbnail))
                {
                    try
                    {
                        File.Copy(codex.Thumbnail, Path.Combine(ThumbnailsPath, $"{codex.ID}.png"), true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"Failed to copy thumbnail of {codex.Title}", ex);
                    }
                }

                //update img path to these new files
                codex.SetImagePaths(this);

                //if no thumbnail file was moved, create one
                if (!File.Exists(codex.Thumbnail))
                {
                    CoverService.CreateThumbnail(codex);
                }

                //move user files included in import
                if (codex.Sources.Path.StartsWith(source.UserFilesPath) && File.Exists(codex.Sources.Path))
                {
                    try
                    {
                        string newPath = codex.Sources.Path.Replace(source.UserFilesPath, UserFilesPath);
                        string? newDir = Path.GetDirectoryName(newPath);
                        if (newDir != null)
                        {
                            try
                            {
                                Directory.CreateDirectory(newDir);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("Failed to create a folder to store the imported files", ex);
                            }
                        }
                        File.Copy(codex.Sources.Path, newPath, true);
                        codex.Sources.Path = newPath;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"Failed to copy file associated with {codex.Title}", ex);
                    }
                }
                AllCodices.Add(codex);
            }
        }

        public void DeleteCodex(Codex toDelete)
        {
            //Delete codex from all lists
            AllCodices.Remove(toDelete);

            //Delete CoverArt & Thumbnail
            try
            {
                File.Delete(toDelete.CoverArt);
                File.Delete(toDelete.Thumbnail);
            }
            catch
            {
                //deleting the thumbnail could fail because of many reasons,
                //not a big deal as it will just get overwritten when a new codex gets the freed id
            }
            Logger.Info($"Removed {toDelete.Title} from {DirectoryName}");
        }

        public void DeleteCodices(IList<Codex> toDelete)
        {
            Notification deleteWarnNotification = Notification.AreYouSureNotification;
            deleteWarnNotification.Body = $"You are about to remove {toDelete.Count} item{(toDelete.Count > 1 ? @"s" : @"")}. " +
                           $"This cannot be undone. " +
                           $"Are you sure you want to continue?";
            var windowedNotificationService = App.Container.ResolveKeyed<INotificationService>(NotificationDisplayType.Windowed);
            windowedNotificationService.Show(deleteWarnNotification);

            if (deleteWarnNotification.Result == NotificationAction.Confirm)
            {
                foreach (Codex toDel in toDelete)
                {
                    DeleteCodex(toDel);
                }
                SaveCodices();
            }
        }

        public void BanishCodices(IList<Codex> toBanish)
        {
            if (toBanish is null) return;
            IEnumerable<string> toBanishPaths = toBanish.Select(codex => codex.Sources.Path);
            IEnumerable<string> toBanishURLs = toBanish.Select(codex => codex.Sources.SourceURL);
            IEnumerable<string> toBanishStrings = toBanishPaths
                .Concat(toBanishURLs)
                .Where(s => !String.IsNullOrWhiteSpace(s))
                .ToHashSet();

            Info.BanishedPaths.AddRange(toBanishStrings);
        }

        public void DeleteTag(Tag toDelete)
        {
            //Recursive loop to delete all children
            if (toDelete.Children.Count > 0)
            {
                DeleteTag(toDelete.Children[0]);
                DeleteTag(toDelete);
            }

            //Remove the tag from all Tags
            AllTags.Remove(toDelete);

            //Remove the tags from parent's children list
            if (toDelete.Parent is null)
            {
                RootTags.Remove(toDelete);
            }
            else
            {
                toDelete.Parent.Children.Remove(toDelete);
            }

            //Remove folder-tag links that contain this tag
            Info.FolderTagPairs.RemoveAll(pair => pair.Tag is not null && pair.Tag == toDelete);

            SaveTags();
        }
    }
}
