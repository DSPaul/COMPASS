using COMPASS.Tools;
using COMPASS.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml;

namespace COMPASS.Models
{
    public class CodexCollection : ObservableObject
    {
        public CodexCollection(string collectionDirectory)
        {
            DirectoryName = collectionDirectory;
        }

        public readonly static string CollectionsPath = Constants.CompassDataPath + @"\Collections\";

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

        //Tag Lists
        public List<Tag> AllTags { get; private set; } = new();
        public List<Tag> RootTags { get; set; }

        //File Lists
        public ObservableCollection<Codex> AllCodices { get; private set; } = new();
        #endregion


        public void Load()
        {
            LoadTags();
            LoadCodices();
            Properties.Settings.Default.StartupCollection = DirectoryName;
        }
        #region Load Data From File
        //Loads the RootTags from a file and constructs the Alltags list from it
        public void LoadTags()
        {
            if (File.Exists(TagsDataFilePath))
            {
                //loading root tags          
                using (var Reader = new StreamReader(TagsDataFilePath))
                {
                    System.Xml.Serialization.XmlSerializer serializer = new(typeof(List<Tag>));
                    RootTags = serializer.Deserialize(Reader) as List<Tag>;
                }

                //Constructing AllTags and pass it to all the tags
                AllTags = Utils.FlattenTree(RootTags).ToList();
                foreach (Tag t in AllTags) t.AllTags = AllTags;
                Logger.Info($"Loaded {RelativeTagsDataFilePath}");
            }
            else
            {
                RootTags = new();
            }
        }

        //Loads AllCodices list from Files
        public void LoadCodices()
        {
            if (File.Exists(CodicesDataFilePath))
            {
                using (var Reader = new StreamReader(CodicesDataFilePath))
                {
                    System.Xml.Serialization.XmlSerializer serializer = new(typeof(ObservableCollection<Codex>));
                    AllCodices = serializer.Deserialize(Reader) as ObservableCollection<Codex>;
                }

                foreach (Codex c in AllCodices)
                {
                    //reconstruct tags from ID's
                    c.Tags = new(AllTags.Where(t => c.TagIDs.Contains(t.ID)));
                }
                Logger.Info($"Loaded {RelativeCodicesDataFilePath}");
            }
            else
            {
                AllCodices = new();
            }
        }
        #endregion

        #region Save Data To XML File

        public void SaveTags()
        {
            using var writer = XmlWriter.Create(TagsDataFilePath, SettingsViewModel.XmlWriteSettings);
            System.Xml.Serialization.XmlSerializer serializer = new(typeof(List<Tag>));
            serializer.Serialize(writer, RootTags);
            Logger.Info($"Saved {RelativeTagsDataFilePath}");
        }

        public void SaveCodices()
        {
            //Copy id's of tags into list for serialisation
            foreach (Codex codex in AllCodices)
            {
                codex.TagIDs = codex.Tags.Select(t => t.ID).ToList();
            }

            using var writer = XmlWriter.Create(CodicesDataFilePath, SettingsViewModel.XmlWriteSettings);
            System.Xml.Serialization.XmlSerializer serializer = new(typeof(ObservableCollection<Codex>));
            serializer.Serialize(writer, AllCodices);
            Logger.Info($"Saved {RelativeCodicesDataFilePath}");
        }

        #endregion    

        public void DeleteCodex(Codex Todelete)
        {
            //Delete file from all lists
            AllCodices.Remove(Todelete);

            //Delete Coverart & Thumbnail
            File.Delete(Todelete.CoverArt);
            File.Delete(Todelete.Thumbnail);
            Logger.Info($"Deleted {Todelete.Title} from {DirectoryName}");
            SaveCodices();
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
