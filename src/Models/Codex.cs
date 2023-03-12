using COMPASS.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;

namespace COMPASS.Models
{
    public class Codex : ObservableObject, IHasID
    {
        //empty constructor for serialization
        public Codex()
        {
            Authors.CollectionChanged += (e, v) => RaisePropertyChanged(nameof(AuthorsAsString));
        }

        public Codex(CodexCollection cc) : this()
        {
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
            ISBN = c.ISBN;
        }

        public bool HasOfflineSource() => !String.IsNullOrWhiteSpace(Path);

        public bool HasOnlineSource() => !String.IsNullOrWhiteSpace(SourceURL);

        public string GetFileType()
        {
            if (HasOfflineSource())
            {
                return System.IO.Path.GetExtension(Path);
            }

            else if (HasOnlineSource())
            {
                // online sources can also also point to file 
                // either hosted on cloud service like Google drive 
                // or services like homebrewery are always .pdf
                // skip this for now though
                return "webpage";
            }

            else
            {
                return null;
            }
        }

        #region Properties

        private string _path;
        public string Path
        {
            get => _path;
            set
            {
                value = Utils.SanitizeXmlString(value);
                SetProperty(ref _path, value);
            }
        }

        private string _title;
        public string Title
        {
            get => _title;
            set
            {
                value = Utils.SanitizeXmlString(value);
                SetProperty(ref _title, value);
                RaisePropertyChanged(nameof(SortingTitle));
            }
        }

        private string _sortingTitle = "";
        [XmlIgnore]
        public string SortingTitle
        {
            get => String.IsNullOrEmpty(_sortingTitle) ? _title : _sortingTitle;
            set => SetProperty(ref _sortingTitle, value);
        }
        //seperate property needed for serialization or it will get _title and save that
        //instead of saving an empty and mirroring _title during runtime
        public string SerializableSortingTitle
        {
            get => _sortingTitle;
            set
            {
                value = Utils.SanitizeXmlString(value);
                SetProperty(ref _sortingTitle, value);
            }
        }

        private ObservableCollection<string> _authors = new();
        public ObservableCollection<string> Authors
        {
            get => _authors;
            set
            {
                SetProperty(ref _authors, value);
                RaisePropertyChanged(nameof(AuthorsAsString));
            }
        }

        public string AuthorsAsString
        {
            get
            {
                string str = Authors.Count switch
                {
                    1 => Authors[0],
                    > 1 => String.Join(", ", Authors.OrderBy(a => a)),
                    _ => ""
                };
                return str;
            }
        }

        private string _publisher;
        public string Publisher
        {
            get => _publisher;
            set
            {
                value = Utils.SanitizeXmlString(value);
                SetProperty(ref _publisher, value);
            }
        }

        private string _version;
        public string Version
        {
            get => _version;
            set
            {
                value = Utils.SanitizeXmlString(value);
                SetProperty(ref _version, value);
            }
        }

        private string _sourceURL;
        public string SourceURL
        {
            get => _sourceURL;
            set
            {
                value = Utils.SanitizeXmlString(value);
                SetProperty(ref _sourceURL, value);
            }
        }

        public int ID { get; set; }

        private string _coverArt;
        public string CoverArt
        {
            get => _coverArt;
            set
            {
                value = Utils.SanitizeXmlString(value);
                SetProperty(ref _coverArt, value);
            }
        }

        private string _thumbnail;
        public string Thumbnail
        {
            get => _thumbnail;
            set
            {
                value = Utils.SanitizeXmlString(value);
                SetProperty(ref _thumbnail, value);
            }
        }

        private bool _physically_Owned;
        public bool Physically_Owned
        {
            get => _physically_Owned;
            set => SetProperty(ref _physically_Owned, value);
        }

        private ObservableCollection<Tag> _tags = new();
        //Don't save all the tags, only save ID's instead
        [XmlIgnoreAttribute]
        public ObservableCollection<Tag> Tags
        {
            get
            {
                List<Tag> orderedTags = new(_tags.OrderBy(t => t.AllTags.IndexOf(t)));
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _tags.Clear(); //will fail when called from non UI thread which happens during import
                    _tags.AddRange(orderedTags);
                });
                return _tags;
            }

            set => SetProperty(ref _tags, value);
        }
        public List<int> TagIDs { get; set; }

        private string _description;
        public string Description
        {
            get => _description;
            set
            {
                value = Utils.SanitizeXmlString(value);
                SetProperty(ref _description, value);
            }
        }

        private DateTime? _releaseDate = null;
        public DateTime? ReleaseDate
        {
            get => _releaseDate;
            set => SetProperty(ref _releaseDate, value);
        }

        private int _rating;
        public int Rating
        {
            get => _rating;
            set => SetProperty(ref _rating, value);
        }

        private int _pageCount;
        public int PageCount
        {
            get => _pageCount;
            set => SetProperty(ref _pageCount, value);
        }

        private DateTime _dateAdded = DateTime.Now;
        public DateTime DateAdded
        {
            get => _dateAdded;
            set => SetProperty(ref _dateAdded, value);
        }

        private DateTime _lastOpened;
        public DateTime LastOpened
        {
            get => _lastOpened;
            set => SetProperty(ref _lastOpened, value);
        }

        private int _openedCount = 0;
        public int OpenedCount
        {
            get => _openedCount;
            set => SetProperty(ref _openedCount, value);
        }

        private bool _favorite;
        public bool Favorite
        {
            get => _favorite;
            set => SetProperty(ref _favorite, value);
        }

        private string _ISBN;
        public string ISBN
        {
            get => _ISBN;
            set
            {
                value = Utils.SanitizeXmlString(value);
                SetProperty(ref _ISBN, value);
            }
        }
        #endregion 
    }
}

