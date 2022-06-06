using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace COMPASS.Models
{
    public class Codex : ObservableObject, IHasID
    {
        public Codex()
        {
            Tags = new ObservableCollection<Tag>();
        }

        public Codex(CodexCollection cc)
        {
            Tags = new ObservableCollection<Tag>();
            ID = Utils.GetAvailableID(cc.AllFiles);
            CoverArt = CodexCollection.CollectionsPath + cc.Folder + @"\CoverArt\" + ID.ToString() + ".png";
            Thumbnail = CodexCollection.CollectionsPath + cc.Folder + @"\Thumbnails\" + ID.ToString() + ".png";
        }

        public void Copy(Codex c)
        {
            Title = c.Title;
            Path = c.Path;
            Author = c.Author;
            Publisher = c.Publisher;
            Version = c.Version;
            SourceURL = c.SourceURL;
            ID = c.ID;
            CoverArt = c.CoverArt;
            Thumbnail = c.Thumbnail;
            Physically_Owned = c.Physically_Owned;
            Description = c.Description;
            ReleaseDate = c.ReleaseDate;
            Rating = c.Rating;
            PageCount = c.PageCount;
            Tags.Clear();
            foreach (Tag t in c.Tags)
            {
                Tags.Add(t);
            }
        }

        public bool HasOfflineSource()
        {
            if (File.Exists(Path)) return true;
            return false;
        }

        public bool HasOnlineSource()
        {
            if (SourceURL == null || SourceURL == "") return false;
            return true;
        }

        #region Properties

        private string _Path;
        public string Path
        {
            get { return _Path; }
            set { SetProperty(ref _Path, value); }
        }

        private string _Title;
        public string Title
        {
            get { return _Title; }
            set { SetProperty(ref _Title, value); }
        }

        private string _Author;
        public string Author
        {
            get { return _Author; }
            set { SetProperty(ref _Author, value); }
        }

        private string _Publisher;
        public string Publisher
        {
            get { return _Publisher; }
            set { SetProperty(ref _Publisher, value); }
        }

        private string _Version;
        public string Version
        {
            get { return _Version; }
            set { SetProperty(ref _Version, value); }
        }

        private string _SourceURL;
        public string SourceURL
        {
            get { return _SourceURL; }
            set { SetProperty(ref _SourceURL, value); }
        }

        private int _ID;
        public int ID
        {
            get { return _ID; }
            set { SetProperty(ref _ID, value); }
        }

        private string _CoverArt;
        public string CoverArt
        {
            get { return _CoverArt; }
            set { SetProperty(ref _CoverArt, value); }
        }

        private string _Thumbnail;
        public string Thumbnail
        {
            get { return _Thumbnail; }
            set { SetProperty(ref _Thumbnail, value); }
        }

        private bool _Physically_Owned;
        public bool Physically_Owned
        {
            get { return _Physically_Owned; }
            set { SetProperty(ref _Physically_Owned, value); }
        }

        private ObservableCollection<Tag> _Tags;
        //Don't save all the tags, only save ID's instead
        [XmlIgnoreAttribute]
        public ObservableCollection<Tag> Tags
        {
            get { return _Tags; }
            set { SetProperty(ref _Tags, value); }
        }
        public List<int> TagIDs;

        private string _Description;
        public string Description
        {
            get { return _Description; }
            set { SetProperty(ref _Description, value); }
        }

        private DateTime? _ReleaseDate = null;
        public DateTime? ReleaseDate
        {
            get { return _ReleaseDate; }
            set { SetProperty(ref _ReleaseDate, value); }
        }

        private int _Rating;
        public int Rating
        {
            get { return _Rating; }
            set { SetProperty(ref _Rating, value); }
        }

        private int _PageCount;
        public int PageCount
        {
            get { return _PageCount; }
            set { SetProperty(ref _PageCount, value); }
        }
        #endregion 
    }
}

