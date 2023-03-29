using COMPASS.Tools;
using COMPASS.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml;

namespace COMPASS.Models
{
    public class CodexCollection : ObservableObject
    {
        public CodexCollection(string collectionDirectory)
        {
            DirectoryName = collectionDirectory;
        }

        public static string CollectionsPath => Path.Combine(SettingsViewModel.CompassDataPath, "Collections");

        #region Properties
        private string _directoryName;
        public string DirectoryName
        {
            get => _directoryName;
            set => SetProperty(ref _directoryName, value);
        }

        public string RelativeCodicesDataFilePath => DirectoryName + @"\CodexInfo.xml";
        public string CodicesDataFilePath => Path.Combine(CollectionsPath, RelativeCodicesDataFilePath);
        private string RelativeTagsDataFilePath => DirectoryName + @"\Tags.xml";
        public string TagsDataFilePath => Path.Combine(CollectionsPath, RelativeTagsDataFilePath);
        private string RelativeCollectionInfoFilePath => DirectoryName + @"\CollectionInfo.xml";
        public string CollectionInfoFilePath => Path.Combine(CollectionsPath, RelativeCollectionInfoFilePath);


        //Tag Lists
        public List<Tag> AllTags { get; private set; } = new();
        public List<Tag> RootTags { get; set; }

        //File Lists
        public ObservableCollection<Codex> AllCodices { get; private set; } = new();

        public CollectionInfo Info { get; private set; } = new();

        #endregion

        #region Load Data From File

        /// <summary>
        /// Loads the collection and sets it as the new default to load on startup
        /// </summary>
        /// <returns>int that gives status: 0 for success, -1 for failed tags, -2 for failed codices, -4 for failed info, or combination of those</returns>
        public int Load()
        {
            int result = 0;
            bool loadedTags = LoadTags();
            bool loadedCodices = LoadCodices();
            bool loadedInfo = LoadInfo();
            if (!loadedTags) { result -= 1; };
            if (!loadedCodices) { result -= 2; };
            if (!loadedInfo) { result -= 4; };
            Properties.Settings.Default.StartupCollection = DirectoryName;
            Logger.Info($"Loaded {DirectoryName}");
            return result;
        }

        //Loads the RootTags from a file and constructs the Alltags list from it
        public bool LoadTags()
        {
            if (File.Exists(TagsDataFilePath))
            {
                //loading root tags          
                using (var Reader = new StreamReader(TagsDataFilePath))
                {
                    System.Xml.Serialization.XmlSerializer serializer = new(typeof(List<Tag>));
                    try
                    {
                        RootTags = serializer.Deserialize(Reader) as List<Tag>;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Could not load {TagsDataFilePath}.", ex);
                        return false;
                    }
                }

                //Constructing AllTags and pass it to all the tags
                AllTags = Utils.FlattenTree(RootTags).ToList();
                foreach (Tag t in AllTags) t.AllTags = AllTags;
            }
            else
            {
                RootTags = new();
            }
            return true;
        }

        //Loads AllCodices list from Files
        public bool LoadCodices()
        {
            if (File.Exists(CodicesDataFilePath))
            {
                using (var Reader = new StreamReader(CodicesDataFilePath))
                {
                    System.Xml.Serialization.XmlSerializer serializer = new(typeof(ObservableCollection<Codex>));
                    try
                    {
                        AllCodices = serializer.Deserialize(Reader) as ObservableCollection<Codex>;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Could not load {CodicesDataFilePath}", ex);
                        return false;
                    }
                }

                AllCodices.CollectionChanged += (e, v) => SaveCodices();

                foreach (Codex c in AllCodices)
                {
                    //reconstruct tags from ID's
                    c.Tags = new(AllTags.Where(t => c.TagIDs.Contains(t.ID)));

                    //double check image location, redundant but got fucked in an update
                    c.setImagePaths(this);
                }
            }
            else
            {
                AllCodices = new();
            }
            return true;
        }

        public bool LoadInfo()
        {
            if (File.Exists(CollectionInfoFilePath))
            {
                using var Reader = new StreamReader(CollectionInfoFilePath);
                System.Xml.Serialization.XmlSerializer serializer = new(typeof(CollectionInfo));
                try
                {
                    Info = serializer.Deserialize(Reader) as CollectionInfo;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Could not load {CollectionInfoFilePath}", ex);
                    return false;
                }
            }
            else
            {
                Info = new();
            }
            return true;
        }
        #endregion

        #region Save Data To XML File

        public void Save()
        {
            SaveTags();
            SaveCodices();
            SaveInfo();
            Properties.Settings.Default.Save();
            Logger.Info($"Saved {DirectoryName}");
        }

        public void SaveTags()
        {
            try
            {
                using var writer = XmlWriter.Create(TagsDataFilePath, SettingsViewModel.XmlWriteSettings);
                System.Xml.Serialization.XmlSerializer serializer = new(typeof(List<Tag>));
                serializer.Serialize(writer, RootTags);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to Save Tags to {TagsDataFilePath}", ex);
            }
        }

        public void SaveCodices()
        {
            //Copy id's of tags into list for serialisation
            foreach (Codex codex in AllCodices)
            {
                codex.TagIDs = codex.Tags.Select(t => t.ID).ToList();
            }

            try
            {
                using var writer = XmlWriter.Create(CodicesDataFilePath, SettingsViewModel.XmlWriteSettings);
                System.Xml.Serialization.XmlSerializer serializer = new(typeof(ObservableCollection<Codex>));
                serializer.Serialize(writer, AllCodices);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to Save Codex Info to {CodicesDataFilePath}", ex);
            }
        }

        public void SaveInfo()
        {
            Info.PrepareSave();
            try
            {
                using var writer = XmlWriter.Create(CollectionInfoFilePath, SettingsViewModel.XmlWriteSettings);
                System.Xml.Serialization.XmlSerializer serializer = new(typeof(CollectionInfo));
                serializer.Serialize(writer, Info);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to Save Tags to {TagsDataFilePath}", ex);
            }
        }

        #endregion    

        public void DeleteCodex(Codex toDelete)
        {
            //Delete file from all lists
            AllCodices.Remove(toDelete);

            //Delete Coverart & Thumbnail
            File.Delete(toDelete.CoverArt);
            File.Delete(toDelete.Thumbnail);
            Logger.Info($"Deleted {toDelete.Title} from {DirectoryName}");
        }

        public void DeleteCodices(IList toDelete)
        {
            List<Codex> toDeleteList = toDelete?.Cast<Codex>().ToList();
            int count = toDeleteList.Count;
            string message = $"You are about to delete {count} file{(count > 1 ? @"s" : @"")}. " +
                           $"This cannot be undone. " +
                           $"Are you sure you want to continue?";
            var result = MessageBox.Show(message, "Delete", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                foreach (Codex toDel in toDeleteList)
                {
                    DeleteCodex(toDel);
                }
            }
        }

        public void BanishCodices(IList toBanish)
        {
            IEnumerable<Codex> toBanishList = toBanish?.Cast<Codex>();
            IEnumerable<string> toBanishPaths = toBanishList.Select(codex => codex.Path);
            IEnumerable<string> toBanishURLs = toBanishList.Select(codex => codex.SourceURL);
            IEnumerable<string> toBanishStrings = toBanishPaths
                .Concat(toBanishURLs)
                .Where(s => !String.IsNullOrWhiteSpace(s))
                .ToHashSet();

            Info.BanishedPaths.AddRange(toBanishStrings);
        }

        public void DeleteTag(Tag todel)
        {
            //Recursive loop to delete all childeren
            if (todel.Children.Count > 0)
            {
                DeleteTag(todel.Children[0]);
                DeleteTag(todel);
            }
            AllTags.Remove(todel);
            //remove from parent items list
            if (todel.Parent is null) RootTags.Remove(todel);
            else todel.Parent.Children.Remove(todel);

            SaveTags();
        }

        public void RenameCollection(string NewCollectionName)
        {
            foreach (Codex codex in AllCodices)
            {
                //Replace folder names in image paths, include leading and ending "\" to avoid replacing wrong things
                codex.CoverArt = codex.CoverArt.Replace(@"\" + DirectoryName + @"\", @"\" + NewCollectionName + @"\");
                codex.Thumbnail = codex.Thumbnail.Replace(@"\" + DirectoryName + @"\", @"\" + NewCollectionName + @"\");
            }
            try
            {
                Directory.Move(CollectionsPath + DirectoryName, CollectionsPath + NewCollectionName);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to move data files from {DirectoryName} to {NewCollectionName}", ex);
            }

            DirectoryName = NewCollectionName;
            Logger.Info($"Renamed  {DirectoryName} to {NewCollectionName}");
        }
    }
}
