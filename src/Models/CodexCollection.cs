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

        //Metadata Lists
        private ObservableCollection<string> _authorList = new();
        public ObservableCollection<string> AuthorList
        {
            get => _authorList;
            set => SetProperty(ref _authorList, value);
        }

        private ObservableCollection<string> _publisherList = new();
        public ObservableCollection<string> PublisherList
        {
            get => _publisherList;
            set => SetProperty(ref _publisherList, value);
        }

        private ObservableCollection<string> _fileTypeList = new();
        public ObservableCollection<string> FileTypeList
        {
            get => _fileTypeList;
            set => SetProperty(ref _fileTypeList, value);
        }
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

                PopulateMetaDataCollections();

                //Can be moved to empty constructor
                foreach (Codex c in AllCodices)
                {
                    //reconstruct tags from ID's
                    foreach (int id in c.TagIDs)
                    {
                        c.Tags.Add(AllTags.First(t => t.ID == id));
                    }

                    //apply sorting titles
                    c.SortingTitle = c.SerializableSortingTitle;
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

        public void PopulateMetaDataCollections()
        {
            foreach(Codex c in AllCodices)
            {
                //Populate Author Collection
                AuthorList = new(AuthorList.Union(c.Authors));
                //Populate Publisher Collection
                if (!String.IsNullOrEmpty(c.Publisher) && !PublisherList.Contains(c.Publisher))
                    PublisherList.Add(c.Publisher);
                //Populate FileType Collection
                string fileType = c.GetFileType(); // to avoid the same function call 3 times
                if (!String.IsNullOrEmpty(fileType) && !FileTypeList.Contains(fileType))
                    FileTypeList.Add(fileType);
            }
            AuthorList.Remove(""); //remove "" author because String.IsNullOrEmpty cannot be called during Union

            //Sort them
            AuthorList = new(AuthorList.Order());
            PublisherList = new(PublisherList.Order());
            FileTypeList = new(FileTypeList.Order());
        }
    }
}
