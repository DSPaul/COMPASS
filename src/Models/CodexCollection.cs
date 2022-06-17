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
        public CodexCollection(string FolderLocation)
        {
            DirectoryName = FolderLocation;

            LoadTags();
            LoadFiles();

            Properties.Settings.Default.StartupCollection = FolderLocation;
        }

        public readonly static string CollectionsPath = Constants.CompassDataPath + @"\Collections\";
        
        #region Properties
        private string _directoryName;
        public string DirectoryName
        {
            get { return _directoryName; }
            set { SetProperty(ref _directoryName, value);}
        }

        public string BooksFilepath => CollectionsPath + DirectoryName + @"\Files.xml";
        public string TagsFilepath =>  CollectionsPath + DirectoryName + @"\Tags.xml";

        //Tag Lists
        public List<Tag> AllTags { get; private set; } = new();
        public List<Tag> RootTags { get; set; }

        //File Lists
        public List<Codex> AllFiles { get; private set; } = new();

        //Metadata Lists
        private ObservableCollection<string> _authorList = new();
        public ObservableCollection<string> AuthorList
        {
            get { return _authorList; }
            set { SetProperty(ref _authorList, value); }
        }

        private ObservableCollection<string> _publisherList = new();
        public ObservableCollection<string> PublisherList
        {
            get { return _publisherList; }
            set { SetProperty(ref _publisherList, value); }
        }
        #endregion

        #region Load Data From File
        //Loads the RootTags from a file and constructs the Alltags list from it
        private void LoadTags()
        {
            if (File.Exists(TagsFilepath))
            {
                //loading root tags          
                using (var Reader = new StreamReader(TagsFilepath))
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

        //Loads AllFiles list from Files
        private void LoadFiles()
        {
            if (File.Exists(BooksFilepath))
            {
                using (var Reader = new StreamReader(BooksFilepath))
                {
                    System.Xml.Serialization.XmlSerializer serializer = new(typeof(List<Codex>));
                    AllFiles = serializer.Deserialize(Reader) as List<Codex>;
                }

                foreach (Codex f in AllFiles)
                {
                    //Populate Author and Publisher List
                    if (!String.IsNullOrEmpty(f.Author) && !AuthorList.Contains(f.Author)) AuthorList.Add(f.Author);
                    if (!String.IsNullOrEmpty(f.Publisher) && !PublisherList.Contains(f.Publisher)) PublisherList.Add(f.Publisher);

                    //reconstruct tags from ID's
                    foreach (int id in f.TagIDs)
                    {
                        f.Tags.Add(AllTags.First(t => t.ID == id));
                    }
                }
                //Sort them
                AuthorList = new(AuthorList.OrderBy(n => n));
                PublisherList = new(PublisherList.OrderBy(n => n));
            }
            else
            {
                AllFiles = new();
            }
        }
        #endregion

        #region Save Data To File

        public void SaveTagsToFile()
        {
            using var writer = XmlWriter.Create(TagsFilepath, SettingsViewModel.XmlWriteSettings);
            System.Xml.Serialization.XmlSerializer serializer = new(typeof(List<Tag>));
            serializer.Serialize(writer, RootTags);
        }

        public void SaveFilesToFile()
        {
            //Copy id's of tags into list for serialisation
            foreach (Codex codex in AllFiles)
            {
                codex.TagIDs = codex.Tags.Select(t => t.ID).ToList();
            }

            using var writer = XmlWriter.Create(BooksFilepath, SettingsViewModel.XmlWriteSettings);
            System.Xml.Serialization.XmlSerializer serializer = new(typeof(List<Codex>));
            serializer.Serialize(writer, AllFiles);
        }

        #endregion    

        public void DeleteFile(Codex Todelete)
        {
            //Delete file from all lists
            AllFiles.Remove(Todelete);

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
            if (todel.ParentID == -1) RootTags.Remove(todel);
            else todel.GetParent().Children.Remove(todel);
        }

        public void RenameCollection(string NewCollectionName)
        {
            Directory.Move(CollectionsPath + DirectoryName, CollectionsPath + NewCollectionName);
            DirectoryName = NewCollectionName;
            foreach(Codex codex in AllFiles)
            {
                //Replace folder names in image paths, include leading and ending "\" to avoid replacing wrong things
                codex.CoverArt  = codex.CoverArt.Replace(@"\" + DirectoryName +@"\", @"\" + NewCollectionName + @"\");
                codex.Thumbnail = codex.Thumbnail.Replace(@"\" + DirectoryName + @"\", @"\" + NewCollectionName + @"\");
            }
        }
    }
}
