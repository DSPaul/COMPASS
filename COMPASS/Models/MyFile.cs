using COMPASS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
namespace COMPASS
{
    public class MyFile : ObservableObject
    {
        public MyFile()
        {
            Tags = new ObservableCollection<Tag>();

            //Give each tag an ID, "try" because can only be done when app has a Currentdata object
            try
            {
                int tempID = 0;
                while (UserSettings.CurrentData.AllFiles.Any(f => f.ID == tempID))
                {
                    tempID++;
                }
                ID = tempID;
                CoverArt = (Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Compass\Collections\" + UserSettings.CurrentData.Folder + @"\CoverArt\" + ID.ToString() + ".png");
            }

            catch { }
        }

        public void Copy(MyFile f)
        {
            Title = f.Title;
            Path = f.Path;
            Author = f.Author;
            Publisher = f.Publisher;
            Version = f.Version;
            SourceURL = f.SourceURL;
            ID = f.ID;
            CoverArt = f.CoverArt;
            Physically_Owned = f.Physically_Owned;
            Tags.Clear();
            foreach (Tag t in f.Tags)
            {
                Tags.Add(t);
            }
        }

        private string _Path;
        private string _Title;
        private string _Author;
        private string _Publisher;
        private string _Version;
        private string _SourceURL;
        private int _ID;
        private string _CoverArt;
        private bool _Physically_Owned;

        public string Path
        {
            get { return _Path; }
            set { SetProperty(ref _Path, value); }
        }
        public string Title
        {
            get { return _Title; }
            set { SetProperty(ref _Title, value); }
        }
        public string Author
        {
            get { return _Author; }
            set { SetProperty(ref _Author, value); }
        }
        public string Publisher
        {
            get { return _Publisher; }
            set { SetProperty(ref _Publisher, value); }
        }
        public string Version
        {
            get { return _Version; }
            set { SetProperty(ref _Version, value); }
        }
        public string SourceURL
        {
            get { return _SourceURL; }
            set { SetProperty(ref _SourceURL, value); }
        }
        public int ID
        {
            get { return _ID; }
            set { SetProperty(ref _ID, value); }
        }
        public string CoverArt
        {
            get { return _CoverArt; }
            set { SetProperty(ref _CoverArt, value); }
        }
        public bool Physically_Owned
        {
            get { return _Physically_Owned; }
            set { SetProperty(ref _Physically_Owned, value); }
        }

        private ObservableCollection<Tag> _Tags;
        public ObservableCollection<Tag> Tags
        {
            get { return _Tags; }
            set { SetProperty(ref _Tags, value); }
        }
    }
}

