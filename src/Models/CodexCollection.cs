using COMPASS.Services;
using COMPASS.Tools;
using COMPASS.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;

namespace COMPASS.Models
{
    public class CodexCollection : ObservableObject
    {
        public CodexCollection(string collectionDirectory)
        {
            DirectoryName = collectionDirectory;
        }

        public static string CollectionsPath => Path.Combine(SettingsViewModel.CompassDataPath, "Collections");
        public string FullDataPath => Path.Combine(CollectionsPath, DirectoryName);
        public string CodicesDataFilePath => Path.Combine(FullDataPath, "CodexInfo.xml");
        public string TagsDataFilePath => Path.Combine(FullDataPath, "Tags.xml");
        public string CollectionInfoFilePath => Path.Combine(FullDataPath, "CollectionInfo.xml");

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
        public List<Tag> RootTags { get; set; }

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
                Properties.Settings.Default.StartupCollection = DirectoryName;
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
                using (var reader = new StreamReader(TagsDataFilePath))
                {
                    XmlSerializer serializer = new(typeof(List<Tag>));
                    try
                    {
                        RootTags = serializer.Deserialize(reader) as List<Tag>;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Could not load {TagsDataFilePath}.", ex);
                        return false;
                    }
                }

                //Constructing AllTags and pass it to all the tags
                AllTags = RootTags.Flatten().ToList();
                foreach (Tag t in AllTags)
                {
                    t.AllTags = AllTags;
                }
            }
            else
            {
                RootTags = new();
            }
            _loadedTags = true;
            return true;
        }

        //Loads AllCodices list from Files
        public bool LoadCodices()
        {
            if (File.Exists(CodicesDataFilePath))
            {
                using (var reader = new StreamReader(CodicesDataFilePath))
                {
                    XmlSerializer serializer = new(typeof(ObservableCollection<Codex>));
                    try
                    {
                        AllCodices = serializer.Deserialize(reader) as ObservableCollection<Codex>;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Could not load {CodicesDataFilePath}", ex);
                        return false;
                    }
                }

                //AllCodices.CollectionChanged += (e, v) => SaveCodices();

                Debug.Assert(AllCodices != null, nameof(AllCodices) + " != null");
                foreach (Codex c in AllCodices)
                {
                    //reconstruct tags from ID's
                    c.Tags = new(AllTags.Where(t => c.TagIDs.Contains(t.ID)));

                    //double check image location, redundant but got fucked in an update
                    c.SetImagePaths(this);
                }
            }
            else
            {
                AllCodices = new();
            }
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
                    Info = serializer.Deserialize(reader) as CollectionInfo;
                    if (Info == null)
                    {
                        Logger.Warn($"Could not load info for {CollectionInfoFilePath}");
                        return false;
                    }
                    Info.CompleteLoading(this);
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
            _loadedInfo = true;
            return true;
        }
        #endregion

        #region Save Data To XML File

        public void CreateDirectories()
        {
            //top folder
            Directory.CreateDirectory(FullDataPath);
            //subfolders
            Directory.CreateDirectory(CoverArtPath);
            Directory.CreateDirectory(ThumbnailsPath);
            Directory.CreateDirectory(UserFilesPath);
        }

        public void Save()
        {
            RaisePropertyChanged(nameof(AllCodices));
            Directory.CreateDirectory(UserFilesPath);
            bool savedTags = SaveTags();
            bool savedCodices = SaveCodices();
            bool savedInfo = SaveInfo();
            Properties.Settings.Default.Save();

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
                using var writer = XmlWriter.Create(TagsDataFilePath, SettingsViewModel.XmlWriteSettings);
                XmlSerializer serializer = new(typeof(List<Tag>));
                serializer.Serialize(writer, RootTags);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to Save Tags to {TagsDataFilePath}", ex);
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
            //Copy id's of tags into list for serialisation
            foreach (Codex codex in AllCodices)
            {
                codex.TagIDs = codex.Tags.Select(t => t.ID).ToList();
            }

            try
            {
                using var writer = XmlWriter.Create(CodicesDataFilePath, SettingsViewModel.XmlWriteSettings);
                XmlSerializer serializer = new(typeof(ObservableCollection<Codex>));
                serializer.Serialize(writer, AllCodices);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to Save Codex Info to {CodicesDataFilePath}", ex);
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
                using var writer = XmlWriter.Create(CollectionInfoFilePath, SettingsViewModel.XmlWriteSettings);
                XmlSerializer serializer = new(typeof(CollectionInfo));
                serializer.Serialize(writer, Info);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to Save Tags to {TagsDataFilePath}", ex);
                return false;
            }
            return true;
        }

        #endregion    

        /// <summary>
        /// Will merge all the data from toMerge into this collection
        /// </summary>
        /// <param name="toMerge"></param>
        public async Task MergeWith(CodexCollection toMerge)
        {
            ImportTags(toMerge.RootTags);
            ImportCodicesFrom(toMerge);
            Info.MergeWith(toMerge.Info);
            if (MainViewModel.CollectionVM.CurrentCollection == this)
            {
                await MainViewModel.CollectionVM.Refresh();
            }
            else
            {
                Save();
            }
        }

        public void ImportTags(IEnumerable<Tag> tags)
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
                Directory.CreateDirectory(UserFilesPath);
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

                //Move or Generate Thumbnail file
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
                else
                {
                    CoverService.CreateThumbnail(codex);
                }

                //update img path to these new files
                codex.SetImagePaths(this);

                //move user files included in import
                if (codex.Path.StartsWith(source.UserFilesPath) && File.Exists(codex.Path))
                {
                    try
                    {
                        string newPath = codex.Path.Replace(source.UserFilesPath, UserFilesPath);
                        File.Copy(codex.Path, newPath, true);
                        codex.Path = newPath;
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
            File.Delete(toDelete.CoverArt);
            File.Delete(toDelete.Thumbnail);
            Logger.Info($"Removed {toDelete.Title} from {DirectoryName}");
        }

        public void DeleteCodices(IList<Codex> toDelete)
        {
            int count = toDelete.Count;
            string message = $"You are about to remove {count} item{(count > 1 ? @"s" : @"")}. " +
                           $"This cannot be undone. " +
                           $"Are you sure you want to continue?";
            var result = MessageBox.Show(message, "Remove", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                foreach (Codex toDel in toDelete)
                {
                    DeleteCodex(toDel);
                }
            }
            SaveCodices();
        }

        public void BanishCodices(IList<Codex> toBanish)
        {
            if (toBanish is null) return;
            IEnumerable<string> toBanishPaths = toBanish.Select(codex => codex.Path);
            IEnumerable<string> toBanishURLs = toBanish.Select(codex => codex.SourceURL);
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
            Info.FolderTagPairs.RemoveAll(pair => pair.Tag == toDelete);

            SaveTags();
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
    }
}
