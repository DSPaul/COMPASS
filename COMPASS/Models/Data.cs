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

        public Tag EditedTag;
        #endregion

        #region File Data
        //File Lists
        public ObservableCollection<MyFile> AllFiles = new ObservableCollection<MyFile>();

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

        public void DeleteFile(MyFile Todelete)
        {
            //Delete file from all lists
            AllFiles.Remove(Todelete);

            //Delete Coverart
            File.Delete(Todelete.CoverArt);
        }

        public void DeleteTag(Tag todel)
        {
            //Recursive loop to delete all childeren
            if (todel.Items.Count > 0)
            {
                DeleteTag(todel.Items[0]);
                DeleteTag(todel);
            }
            AllTags.Remove(todel);
            //remove from parent items list
            if (todel.ParentID == -1) RootTags.Remove(todel);
            else todel.GetParent().Items.Remove(todel);
        }
    }
}
