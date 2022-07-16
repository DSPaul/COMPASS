using COMPASS.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Serialization;
using System.Linq;

namespace COMPASS.Models
{
    public class Codex : ObservableObject, IHasID
    {
        //empty constructor for serialization
        public Codex() 
        {
            Authors.CollectionChanged += (e, v) => RaisePropertyChanged(nameof(AuthorsAsString));
        }

        public Codex(CodexCollection cc):this()
        {
            Tags = new();
            ID = Utils.GetAvailableID(cc.AllCodices);
            CoverArt = CodexCollection.CollectionsPath + cc.DirectoryName + @"\CoverArt\" + ID.ToString() + ".png";
            Thumbnail = CodexCollection.CollectionsPath + cc.DirectoryName + @"\Thumbnails\" + ID.ToString() + ".png";

        }

        public void Copy(Codex c)
        {
            Title = c.Title;
            _sortingTitle = c._sortingTitle; //copy field instead of property, or it will copy _title
            Path = c.Path;
            Authors = new(c.Authors);
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
            Tags = new(c.Tags);
            LastOpened = c.LastOpened;
            DateAdded = c.DateAdded;
            Favorite = c.Favorite;
            OpenedCount = c.OpenedCount;
        }

        public bool HasOfflineSource()
        {
            return File.Exists(Path);
        }

        public bool HasOnlineSource()
        {
            return !string.IsNullOrEmpty(SourceURL);
        }

        #region Properties

        private string _path;
        public string Path
        {
            get { return _path; }
            set { SetProperty(ref _path, value); }
        }

        private string _title;
        public string Title
        {
            get { return _title; }
            set 
            { 
                SetProperty(ref _title, value);
                RaisePropertyChanged(nameof(SortingTitle));
            }
        }

        private string _sortingTitle = "";
        [XmlIgnoreAttribute]
        public string SortingTitle
        {
            get 
            {
                if (String.IsNullOrEmpty(_sortingTitle)) return _title;
                else return _sortingTitle;
            }
            set { SetProperty(ref _sortingTitle, value); }
        }
        //seperate property needed for serialization or it will get _title and save that
        //instead of saving an empty and mirroring _title during runtime
        public string SerializableSortingTitle
        {
            get { return _sortingTitle; }
            set { SetProperty(ref _sortingTitle, value); }
        }

        private ObservableCollection<string> _authors = new();
        public ObservableCollection<string> Authors
        {
            get { return _authors; }
            set 
            { 
                SetProperty(ref _authors, value);
                RaisePropertyChanged(nameof(AuthorsAsString));
            }
        }

        public string AuthorsAsString {
            get
            {
                string str = Authors.Count switch
                {
                    1 => Authors[0],
                    > 1 => String.Join(", ", Authors.OrderBy(a=>a)),
                    _ => ""
                };
                return str;
            }            
        }

        private string _publisher;
        public string Publisher
        {
            get { return _publisher; }
            set { SetProperty(ref _publisher, value); }
        }

        private string _version;
        public string Version
        {
            get { return _version; }
            set { SetProperty(ref _version, value); }
        }

        private string _sourceURL;
        public string SourceURL
        {
            get { return _sourceURL; }
            set { SetProperty(ref _sourceURL, value); }
        }

        public int ID { get; set; }

        private string _coverArt;
        public string CoverArt
        {
            get { return _coverArt; }
            set { SetProperty(ref _coverArt, value); }
        }

        private string _thumbnail;
        public string Thumbnail
        {
            get { return _thumbnail; }
            set { SetProperty(ref _thumbnail, value); }
        }

        private bool _physically_Owned;
        public bool Physically_Owned
        {
            get { return _physically_Owned; }
            set { SetProperty(ref _physically_Owned, value); }
        }

        private ObservableCollection<Tag> _tags = new();
        //Don't save all the tags, only save ID's instead
        [XmlIgnoreAttribute]
        public ObservableCollection<Tag> Tags
        {
            get { return _tags; }
            set { SetProperty(ref _tags, value); }
        }
        public List<int> TagIDs { get; set; }

        private string _description;
        public string Description
        {
            get { return _description; }
            set { SetProperty(ref _description, value); }
        }

        private DateTime? _releaseDate = null;
        public DateTime? ReleaseDate
        {
            get { return _releaseDate; }
            set { SetProperty(ref _releaseDate, value); }
        }

        private int _rating;
        public int Rating
        {
            get { return _rating; }
            set { SetProperty(ref _rating, value); }
        }

        private int _pageCount;
        public int PageCount
        {
            get { return _pageCount; }
            set { SetProperty(ref _pageCount, value); }
        }

        private DateTime _dateAdded = DateTime.Now;
        public DateTime DateAdded
        {
            get { return _dateAdded; }
            set { SetProperty(ref _dateAdded, value); }
        }

        private DateTime _lastOpened;
        public DateTime LastOpened
        {
            get { return _lastOpened; }
            set { SetProperty(ref _lastOpened,value); }
        }

        private int _openedCount = 0;
        public int OpenedCount
        {
            get { return _openedCount; }
            set { SetProperty(ref _openedCount, value); }
        }

        private bool _favorite;
        public bool Favorite
        {
            get { return _favorite; }
            set { SetProperty(ref _favorite, value); }
        }
        #endregion 
    }
}

