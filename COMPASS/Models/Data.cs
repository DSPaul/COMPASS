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

            AuthorList = new ObservableCollection<string>();
            PublisherList = new ObservableCollection<string>();

            _BooksFilepath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Compass\Collections\" + Folder + @"\Files.xml";
            _TagsFilepath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Compass\Collections\" + Folder + @"\Tags.xml";

            LoadTags();
            LoadFiles();
        }

        private String _Folder;
        readonly String _BooksFilepath;
        readonly String _TagsFilepath;

        public String Folder
        {
            get { return _Folder; }
            set { SetProperty(ref _Folder, value);}
        }

        #region Tag Data
        //Tag Lists
        public ObservableCollection<Tag> AllTags = new ObservableCollection<Tag>();
        private ObservableCollection<Tag> rootTags;
        public ObservableCollection<Tag> RootTags
        {
            get { return rootTags; }
            set { SetProperty(ref rootTags, value); }
        }

        #endregion

        #region File Data
        //File Lists
        public ObservableCollection<MyFile> AllFiles = new ObservableCollection<MyFile>();

        #endregion

        #region Metadata Data
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
                    t.SetAllTags(AllTags);
                    if (t.Items.Count > 0)
                    {
                        foreach (Tag t2 in t.Items) Currentlist.Add(t2);
                    }
                }
            }
            else
            {
                RootTags = new ObservableCollection<Tag>();
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

                foreach (MyFile f in AllFiles)
                {
                    //Populate Author and Publisher List
                    if (f.Author != "" && !AuthorList.Contains(f.Author)) AuthorList.Add(f.Author);
                    if (f.Publisher != "" && !PublisherList.Contains(f.Publisher)) PublisherList.Add(f.Publisher);

                    //Replace Serialized Tags with Tags From Taglist
                    int TagCount = f.Tags.Count;
                    for (int i = 0; i < TagCount; i++)
                    {
                        Tag temp = f.Tags[0];
                        f.Tags.Add(AllTags.First(t => t.ID == temp.ID));
                        f.Tags.Remove(temp);
                    }
                }
                //Sort them
                AuthorList = new ObservableCollection<string>(AuthorList.OrderBy(n => n));
                PublisherList = new ObservableCollection<string>(PublisherList.OrderBy(n => n));
            }
            else
            {
                AllFiles = new ObservableCollection<MyFile>();
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
