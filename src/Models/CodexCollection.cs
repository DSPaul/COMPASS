using COMPASS.Tools;
using COMPASS.ViewModels;
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

            LoadTags();
            LoadCodices();

            Properties.Settings.Default.StartupCollection = collectionDirectory;
        }

        public readonly static string CollectionsPath = Constants.CompassDataPath + @"\Collections\";

        #region Properties
        private string _directoryName;
        public string DirectoryName
        {
            get => _directoryName;
            set => SetProperty(ref _directoryName, value);
        }

        public string CodicesDataFilePath => CollectionsPath + DirectoryName + @"\CodexInfo.xml";
        public string TagsDataFilepath => CollectionsPath + DirectoryName + @"\Tags.xml";

        //Tag Lists
        public List<Tag> AllTags { get; private set; } = new();
        public List<Tag> RootTags { get; set; }

        //File Lists
        public ObservableCollection<Codex> AllCodices { get; private set; } = new();
        #endregion

        #region Load Data From File
        //Loads the RootTags from a file and constructs the Alltags list from it
        private void LoadTags()
        {
            if (File.Exists(TagsDataFilepath))
            {
                //loading root tags          
                using (var Reader = new StreamReader(TagsDataFilepath))
                {
                    System.Xml.Serialization.XmlSerializer serializer = new(typeof(List<Tag>));
                    RootTags = serializer.Deserialize(Reader) as List<Tag>;
                }

                //Constructing AllTags and pass it to all the tags
                AllTags = Utils.FlattenTree(RootTags).ToList();
                foreach (Tag t in AllTags) t.AllTags = AllTags;
            }
            else
            {
                RootTags = new();
            }
        }

        //Loads AllCodices list from Files
        private void LoadCodices()
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
            using var writer = XmlWriter.Create(TagsDataFilepath, SettingsViewModel.XmlWriteSettings);
            System.Xml.Serialization.XmlSerializer serializer = new(typeof(List<Tag>));
            serializer.Serialize(writer, RootTags);
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
        }

        #endregion    

        public void DeleteCodex(Codex Todelete)
        {
            //Delete file from all lists
            AllCodices.Remove(Todelete);

            //Delete Coverart & Thumbnail
            File.Delete(Todelete.CoverArt);
            File.Delete(Todelete.Thumbnail);
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
        }

        public void RenameCollection(string NewCollectionName)
        {
            foreach (Codex codex in AllCodices)
            {
                //Replace folder names in image paths, include leading and ending "\" to avoid replacing wrong things
                codex.CoverArt = codex.CoverArt.Replace(@"\" + DirectoryName + @"\", @"\" + NewCollectionName + @"\");
                codex.Thumbnail = codex.Thumbnail.Replace(@"\" + DirectoryName + @"\", @"\" + NewCollectionName + @"\");
            }
            Directory.Move(CollectionsPath + DirectoryName, CollectionsPath + NewCollectionName);
            DirectoryName = NewCollectionName;
        }
    }
}
