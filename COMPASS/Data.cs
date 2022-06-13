using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace COMPASS
{
    class Data
    {
        #region Tag Data
        //Tag Lists
        public static ObservableCollection<Tag> AllTags = new ObservableCollection<Tag>();
        public static ObservableCollection<Tag> RootTags = new ObservableCollection<Tag>();
        public static ObservableCollection<Tag> ActiveTags = new ObservableCollection<Tag>();

        public static Tag EditedTag = new Tag();
        #endregion

        #region File Data
        //File Lists
        public static ObservableCollection<MyFile> AllFiles = new ObservableCollection<MyFile>();
        public static ObservableCollection<MyFile> TagFilteredFiles = new ObservableCollection<MyFile>(AllFiles);
        public static ObservableCollection<MyFile> SearchFilteredFiles = new ObservableCollection<MyFile>(AllFiles);
        public static ObservableCollection<MyFile> ActiveFiles = new ObservableCollection<MyFile>(AllFiles);

        public static MyFile EditedFile = new MyFile();
        #endregion

        #region Metadata Data
        //Metadata Lists
        public static ObservableCollection<String> AuthorList = new ObservableCollection<string>();
        public static ObservableCollection<String> SourceList = new ObservableCollection<string>();
        public static ObservableCollection<String> PublisherList = new ObservableCollection<string>();
        #endregion

        #region Load Data From File
        //Loads the RootTags from a file and constructs the Alltags list from it
        public static void LoadTags()
        {
            if (File.Exists(@"C:\Users\pauld\Documents\Tags.xml"))
            {
                //loading root tags
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(ObservableCollection<Tag>));
                using (var Reader = new StreamReader(@"C:\Users\pauld\Documents\Tags.xml"))
                {
                    RootTags = serializer.Deserialize(Reader) as ObservableCollection<Tag>;
                }

                //Creating All Tags
                List<Tag> Currentlist = RootTags.ToList();
                for (int i = 0; i < Currentlist.Count(); i++)
                {
                    Tag t = Currentlist[i];
                    Data.AllTags.Add(t);
                    if (t.Items.Count > 0)
                    {
                        foreach (Tag t2 in t.Items) Currentlist.Add(t2);
                    }
                }
            }
        }

        //Loads AllFiles list from Files
        public static void LoadFiles()
        {
            if (File.Exists(@"C:\Users\pauld\Documents\Files.xml"))
            {
                System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(ObservableCollection<MyFile>));
                using (var Reader = new StreamReader(@"C:\Users\pauld\Documents\Files.xml"))
                {
                    AllFiles = serializer.Deserialize(Reader) as ObservableCollection<MyFile>;
                }

                //build metadatalists
                foreach (MyFile f in AllFiles)
                {
                    if (f.Author != "" && !AuthorList.Contains(f.Author)) AuthorList.Add(f.Author);
                    if (f.Publisher != "" && !PublisherList.Contains(f.Publisher)) PublisherList.Add(f.Publisher);
                    if (f.Source != "" && !SourceList.Contains(f.Source)) SourceList.Add(f.Source);
                }
            }
            Rebuild_FileData();
        }
        #endregion

        #region File Functions
        //Reset all Filtered File Lists to include all Files
        public static void Rebuild_FileData()
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
        public static void Update_ActiveFiles()
        {
            ActiveFiles.Clear();
            foreach (var p in TagFilteredFiles.Intersect(SearchFilteredFiles))
                ActiveFiles.Add(p);
        }

        public static void DeleteFile(MyFile Todelete)
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
        public static void Deactivate_Tag(Tag ToDeactivate)
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
