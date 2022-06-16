using COMPASS.Tools;
using COMPASS.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml;

namespace COMPASS.Models
{
    public class CodexCollection : ObservableObject
    {
        public CodexCollection(string FolderLocation)
        {
            Folder = FolderLocation;

            AuthorList = new ObservableCollection<string>();
            PublisherList = new ObservableCollection<string>();

            LoadTags();
            LoadFiles();

            Properties.Settings.Default.StartupCollection = FolderLocation;
        }

        public readonly static string CollectionsPath = Constants.CompassDataPath + @"\Collections\";
        
        #region Properties
        private String _Folder;
        public String Folder
        {
            get { return _Folder; }
            set { SetProperty(ref _Folder, value);}
        }

        public String BooksFilepath
        {
            get { return CollectionsPath + Folder + @"\Files.xml"; ; }
        }

        public String TagsFilepath
        {
            get { return CollectionsPath + Folder + @"\Tags.xml"; ; }
        }

        //Tag Lists
        public List<Tag> AllTags = new List<Tag>();
        public List<Tag> RootTags;

        //File Lists
        public List<Codex> AllFiles = new List<Codex>();

        //Metadata Lists
        private ObservableCollection<String> authorList;
        public ObservableCollection<String> AuthorList
        {
            get { return authorList; }
            set { SetProperty(ref authorList, value); }
        }

        private ObservableCollection<String> publisherList;
        public ObservableCollection<String> PublisherList
        {
            get { return publisherList; }
            set { SetProperty(ref publisherList, value); }
        }
        #endregion

        #region Load Data From File
        //Loads the RootTags from a file and constructs the Alltags list from it
        public void LoadTags()
        {
            if (File.Exists(TagsFilepath))
            {
                //loading root tags          
                using (var Reader = new StreamReader(TagsFilepath))
                {
                    System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(List<Tag>));
                    RootTags = serializer.Deserialize(Reader) as List<Tag>;
                    Reader.Close();
                }

                //Constructing AllTags and pass it to all the tags
                AllTags = Utils.FlattenTree(RootTags).ToList();
                foreach (Tag t in AllTags) t.AllTags = AllTags;
            }
            else
            {
                RootTags = new List<Tag>();
            }
        }

        //Loads AllFiles list from Files
        public void LoadFiles()
        {
            if (File.Exists(BooksFilepath))
            {
                using (var Reader = new StreamReader(BooksFilepath))
                {
                    System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(List<Codex>));
                    AllFiles = serializer.Deserialize(Reader) as List<Codex>;
                    Reader.Close();
                }

                foreach (Codex f in AllFiles)
                {
                    //Populate Author and Publisher List
                    if (f.Author != "" && !AuthorList.Contains(f.Author)) AuthorList.Add(f.Author);
                    if (f.Publisher != "" && !PublisherList.Contains(f.Publisher)) PublisherList.Add(f.Publisher);

                    //reconstruct tags from ID's
                    foreach (int id in f.TagIDs)
                    {
                        f.Tags.Add(AllTags.First(t => t.ID == id));
                    }
                }
                //Sort them
                AuthorList = new ObservableCollection<string>(AuthorList.OrderBy(n => n));
                PublisherList = new ObservableCollection<string>(PublisherList.OrderBy(n => n));
            }
            else
            {
                AllFiles = new List<Codex>();
            }
        }
        #endregion

        #region Save Data To File

        public void SaveTagsToFile()
        {
            using (var writer = XmlWriter.Create(TagsFilepath, SettingsViewModel.xmlWriterSettings))
            {
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(List<Tag>));
                serializer.Serialize(writer, RootTags);
            }
        }

        public void SaveFilesToFile()
        {
            //Copy id's of tags into list for serialisation
            foreach (Codex codex in AllFiles)
            {
                codex.TagIDs = codex.Tags.Select(t => t.ID).ToList();
            }

            using (var writer = XmlWriter.Create(BooksFilepath, SettingsViewModel.xmlWriterSettings))
            {
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(List<Codex>));
                serializer.Serialize(writer, AllFiles);
            }
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

        public void RenameFolder(string NewFoldername)
        {
            Directory.Move(CollectionsPath + Folder, CollectionsPath + NewFoldername);
            Folder = NewFoldername;
            foreach(Codex file in AllFiles)
            {
                //Replace folder names in image paths, include leading and ending "\" to avoid replacing wrong things
                file.CoverArt  = file.CoverArt.Replace(@"\" + Folder +@"\", @"\" + NewFoldername + @"\");
                file.Thumbnail = file.Thumbnail.Replace(@"\" + Folder + @"\", @"\" + NewFoldername + @"\");
            }
        }
    }
}
