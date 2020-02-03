using COMPASS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace COMPASS
{
    public class Data : ObservableObject
    {
        public Data(String FolderLocation)
        {
            Folder = FolderLocation;

            _BooksFilepath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Compass\Collections\" + Folder + @"\Files.xml";
            _TagsFilepath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Compass\Collections\" + Folder + @"\Tags.xml";

            LoadFiles();
            LoadTags();

            TagFilteredFiles = new ObservableCollection<MyFile>(AllFiles);
            SearchFilteredFiles = new ObservableCollection<MyFile>(AllFiles);
            ActiveFiles = new ObservableCollection<MyFile>(AllFiles);
        }

        private String _Folder;
        private String _BooksFilepath;
        private String _TagsFilepath;

        public String Folder
        {
            get { return _Folder; }
            set { SetProperty(ref _Folder, value);}
        }

        #region Tag Data
        //Tag Lists
        public ObservableCollection<Tag> AllTags = new ObservableCollection<Tag>();
        public ObservableCollection<Tag> RootTags = new ObservableCollection<Tag>();
        public ObservableCollection<Tag> ActiveTags = new ObservableCollection<Tag>();

        public Tag EditedTag;
        #endregion

        #region File Data
        //File Lists
        public ObservableCollection<MyFile> AllFiles = new ObservableCollection<MyFile>();
        public ObservableCollection<MyFile> TagFilteredFiles;
        public ObservableCollection<MyFile> SearchFilteredFiles;
        public ObservableCollection<MyFile> ActiveFiles { get; set; }

        public MyFile EditedFile;
        #endregion

        #region Metadata Data
        //Metadata Lists
        public ObservableCollection<String> AuthorList = new ObservableCollection<string>();
        public ObservableCollection<String> PublisherList = new ObservableCollection<string>();
        #endregion

        #region Load Data From File
        //Loads the RootTags from a file and constructs the Alltags list from it
        public void LoadTags()
        {
            if (File.Exists(_TagsFilepath))
            {
                //loading root tags
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(ObservableCollection<Tag>));
                using (var Reader = new StreamReader(_TagsFilepath))
                {
                    this.RootTags = serializer.Deserialize(Reader) as ObservableCollection<Tag>;
                }

                //Creating All Tags
                List<Tag> Currentlist = RootTags.ToList();
                for (int i = 0; i < Currentlist.Count(); i++)
                {
                    Tag t = Currentlist[i];
                    AllTags.Add(t);
                    if (t.Items.Count > 0)
                    {
                        foreach (Tag t2 in t.Items) Currentlist.Add(t2);
                    }
                }
            }
        }

        //Loads AllFiles list from Files
        public void LoadFiles()
        {
            if (File.Exists(_BooksFilepath))
            {
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(ObservableCollection<MyFile>));
                using (var Reader = new StreamReader(_BooksFilepath))
                {
                    AllFiles = serializer.Deserialize(Reader) as ObservableCollection<MyFile>;
                }

                //build metadatalists
                foreach (MyFile f in AllFiles)
                {
                    if (f.Author != "" && !AuthorList.Contains(f.Author)) AuthorList.Add(f.Author);
                    if (f.Publisher != "" && !PublisherList.Contains(f.Publisher)) PublisherList.Add(f.Publisher);
                }
            }
        }
        #endregion

        #region Save Data To File

        public void SaveTagsToFile()
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(ObservableCollection<Tag>));
            using (var writer = new StreamWriter(_TagsFilepath))
            {
                serializer.Serialize(writer, RootTags);
            }
        }

        public void SaveFilesToFile()
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(ObservableCollection<MyFile>));
            using (var writer = new StreamWriter(_BooksFilepath))
            {
                serializer.Serialize(writer, AllFiles);
            }
        }

        #endregion 

        #region File Functions
        //Reset all Filtered File Lists to include all Files
        public void Rebuild_FileData()
        {
            TagFilteredFiles.Clear();
            SearchFilteredFiles.Clear();
            foreach (MyFile f in AllFiles)
            {
                SearchFilteredFiles.Add(f);
                TagFilteredFiles.Add(f);
            }
            Update_ActiveFiles();
        }

        //Construct ActiveFiles by taking the intersection of Tagfilered and Searchfiltered files
        public void Update_ActiveFiles()
        {
            ActiveFiles.Clear();
            foreach (var p in TagFilteredFiles.Intersect(SearchFilteredFiles))
                ActiveFiles.Add(p);
        }

        public void DeleteFile(MyFile Todelete)
        {
            //Delete file from all lists
            AllFiles.Remove(Todelete);
            TagFilteredFiles.Remove(Todelete);
            SearchFilteredFiles.Remove(Todelete);
            
            Update_ActiveFiles();

            //Delete Coverart
            File.Delete(Todelete.CoverArt);
        }
        #endregion

        #region Tag Functions
        public void Deactivate_Tag(Tag ToDeactivate)
        {
            ActiveTags.Remove(ToDeactivate);
            //foreach (Tag t in ActiveTags) if (t == ToDeactivate) ActiveTags.Remove(t);
            TagFilteredFiles.Clear();
            foreach (MyFile f in AllFiles)
            {
                if (ActiveTags.All(i => f.Tags.Contains(i)))
                    TagFilteredFiles.Add(f);
            }
            Update_ActiveFiles();
        }
        #endregion
    }
}
