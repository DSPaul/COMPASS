using COMPASS.Tools;
using COMPASS.ViewModels.Sources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
            SetImagePaths(cc);
        }

        public Codex(Codex codex) : this()
        {
            Copy(codex);
        }

        public void SetImagePaths(CodexCollection collection) => SetImagePaths(collection.DirectoryName);

        public void SetImagePaths(string collectionName)
        {
            CoverArt = System.IO.Path.Combine(CodexCollection.CollectionsPath, collectionName, "CoverArt", $"{ID}.png");
            Thumbnail = System.IO.Path.Combine(CodexCollection.CollectionsPath, collectionName, "Thumbnails", $"{ID}.png");
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

        public void RefreshThumbnail() => RaisePropertyChanged(nameof(Thumbnail));

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

        public static readonly List<CodexProperty> Properties = new()
        {
            new( "Title",
                codex => String.IsNullOrWhiteSpace(codex.Title),
                (codex,other) => codex.Title = other.Title,
                new List<NamedMetaDataSource>()
                {
                    new(MetaDataSource.File),
                    new(MetaDataSource.PDF),
                    new(MetaDataSource.GmBinder),
                    new(MetaDataSource.Homebrewery),
                    new(MetaDataSource.GoogleDrive),
                    new(MetaDataSource.ISBN),
                    new(MetaDataSource.GenericURL)
                }),
            new( "Authors",
                codex => codex.Authors is null || !codex.Authors.Any(),
                (codex,other) => codex.Authors = other.Authors,
                new()
                {
                    new(MetaDataSource.PDF),
                    new(MetaDataSource.GmBinder),
                    new(MetaDataSource.Homebrewery),
                    new(MetaDataSource.ISBN),
                    new(MetaDataSource.GenericURL)
                }),
            new( "Publisher",
                codex => String.IsNullOrEmpty(codex.Publisher),
                (codex,other) => codex.Publisher = other.Publisher,
                new()
                {
                    new(MetaDataSource.ISBN),
                    new(MetaDataSource.GmBinder),
                    new(MetaDataSource.Homebrewery),
                    new(MetaDataSource.GoogleDrive),
                }),
            new( "Version",
                codex => String.IsNullOrEmpty(codex.Version),
                (codex,other) => codex.Version = other.Version,
                new()
                {
                    new(MetaDataSource.Homebrewery)
                }),
            new( "Pagecount",
                codex => codex.PageCount == 0,
                (codex, other) => codex.PageCount = other.PageCount,
                new()
                {
                    new(MetaDataSource.PDF),
                    new(MetaDataSource.Image),
                    new(MetaDataSource.GmBinder),
                    new(MetaDataSource.Homebrewery),
                    new(MetaDataSource.ISBN),
                }),
            new( "Tags",
                codex => codex.Tags is null || !codex.Tags.Any(),
                (codex,other) => {
                    foreach (var tag in other.Tags)
                    {
                        Application.Current.Dispatcher.Invoke(() => codex.Tags.AddIfMissing(tag));
                    }
                },
                new()
                {
                    new(MetaDataSource.File),
                    new(MetaDataSource.GenericURL),
                }),
            new( "Description",
                codex => String.IsNullOrEmpty(codex.Description),
                (codex,other) => codex.Description = other.Description,
                new()
                {
                    new(MetaDataSource.Homebrewery),
                    new(MetaDataSource.ISBN),
                    new(MetaDataSource.GenericURL),
                }),
            new( "Release Date",
                codex => codex.ReleaseDate is null || codex.ReleaseDate == DateTime.MinValue,
                (codex, other) => codex.ReleaseDate = other.ReleaseDate,
                new()
                {
                    new(MetaDataSource.Homebrewery),
                    new(MetaDataSource.ISBN),
                }),
            new( "Cover Art",
                codex => !File.Exists(codex.CoverArt),
                (codex,other) => { }, //cover art is always the same, can't really be set, prop copy from temp here or something
                new()
                {
                    new(MetaDataSource.Image),
                    new(MetaDataSource.PDF),
                    new(MetaDataSource.GmBinder),
                    new(MetaDataSource.Homebrewery),
                    new(MetaDataSource.GoogleDrive),
                    new(MetaDataSource.ISBN),
                }),
        };
    }
}

