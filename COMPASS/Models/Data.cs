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

            LoadTags();
            LoadFiles();
        }

        private String _Folder;

        public String Folder
        {
            get { return _Folder; }
            set { SetProperty(ref _Folder, value);}
        }

        public String BooksFilepath
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Compass\Collections\" + Folder + @"\Files.xml"; ; }
        }

        public String TagsFilepath
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Compass\Collections\" + Folder + @"\Tags.xml"; ; }
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
        public ObservableCollection<Codex> AllFiles = new ObservableCollection<Codex>();

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
            if (File.Exists(TagsFilepath))
            {
                //loading root tags          
                using (var Reader = new StreamReader(TagsFilepath))
                {
                    System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(ObservableCollection<Tag>));
                    this.RootTags = serializer.Deserialize(Reader) as ObservableCollection<Tag>;
                    Reader.Close();
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
            if (File.Exists(BooksFilepath))
            {
                using (var Reader = new StreamReader(BooksFilepath))
                {
                    System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(ObservableCollection<Codex>));
                    AllFiles = serializer.Deserialize(Reader) as ObservableCollection<Codex>;
                    Reader.Close();
                }

                foreach (Codex f in AllFiles)
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
                AllFiles = new ObservableCollection<Codex>();
            }
        }
        #endregion

        #region Save Data To File

        public void SaveTagsToFile()
        {
            using (var writer = new StreamWriter(TagsFilepath))
            {
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(ObservableCollection<Tag>));
                serializer.Serialize(writer, RootTags);
            }
        }

        public void SaveFilesToFile()
        {
            using (var writer = new StreamWriter(BooksFilepath))
            {
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(ObservableCollection<Codex>));
                serializer.Serialize(writer, AllFiles);
            }
        }

        #endregion 

        public void DeleteFile(Codex Todelete)
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

        public void RenameFolder(string NewFoldername)
        {
            Directory.Move(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Compass\Collections\" + Folder, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Compass\Collections\" + NewFoldername);
            Folder = NewFoldername;
            foreach(Codex file in AllFiles)
            {
                file.CoverArt = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Compass\Collections\" + NewFoldername + @"\CoverArt\" + file.ID + ".png";
            }
        }
    }
}
