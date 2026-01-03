using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Interfaces;

namespace COMPASS.Common.Models
{
    public class Codex : ObservableObject, IHasId, IHasCodexMetadata, ICloneable<Codex>
    {
        public readonly CodexCollection Collection;

        #region Constructors

        public Codex(CodexCollection collection)
        {
            Collection = collection;
        }

        private Codex(Codex codex) : this(codex.Collection)
        {
            CopyFrom(codex);
        }

        #endregion

        #region Properties

        #region COMPASS related Metadata

        public int Id { get; set; }

        private string _coverArtPath = "";
        public string CoverArtPath
        {
            get => _coverArtPath;
            set => SetProperty(ref _coverArtPath, value);
        }

        private string _thumbnailPath = "";
        public string ThumbnailPath
        {
            get => _thumbnailPath;
            set => SetProperty(ref _thumbnailPath, value);
        }

        #endregion

        #region Codex related Metadata

        private string _title = "";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _userDefinedSortingTitle = "";
        /// <summary>
        /// Sorting title defined by the user, will only have a value if it is different from the title
        /// </summary>
        public string UserDefinedSortingTitle => _userDefinedSortingTitle;
        public string SortingTitle
        {
            get => (string.IsNullOrEmpty(_userDefinedSortingTitle) ? _title : _userDefinedSortingTitle).PadNumbers();
            set =>  SetProperty(ref _userDefinedSortingTitle, value);
        }

        private ObservableCollection<string> _authors = [];
        public ObservableCollection<string> Authors
        {
            get => _authors;
            set => SetProperty(ref _authors, value);
        }

        private string _publisher = "";
        public string Publisher
        {
            get => _publisher;
            set => SetProperty(ref _publisher, value);
        }

        private string _description = "";
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private DateTime? _releaseDate;
        public DateTime? ReleaseDate
        {
            get => _releaseDate;
            set => SetProperty(ref _releaseDate, value);
        }

        private int _pageCount;
        public int PageCount
        {
            get => _pageCount;
            set => SetProperty(ref _pageCount, value);
        }

        private string _version = "";
        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        #endregion

        #region User related Metadata

        private RangeObservableCollection<Tag> _tags = [];
        public RangeObservableCollection<Tag> Tags
        {
            get => _tags;
            set => SetProperty(ref _tags, value);
        }

        private bool _physicallyOwned;
        public bool PhysicallyOwned
        {
            get => _physicallyOwned;
            set => SetProperty(ref _physicallyOwned, value);
        }

        private int _rating;
        public int Rating
        {
            get => _rating;
            set => SetProperty(ref _rating, value);
        }

        private bool _favorite;
        public bool Favorite
        {
            get => _favorite;
            set => SetProperty(ref _favorite, value);
        }

        #endregion

        #region User behaviour metadata

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

        private int _openedCount;
        public int OpenedCount
        {
            get => _openedCount;
            set => SetProperty(ref _openedCount, value);
        }

        #endregion

        private SourceSet _sources = new();
        public SourceSet Sources
        {
            get => _sources;
            set => SetProperty(ref _sources, value);
        }
        #endregion

        #region Methods
        public void CopyFrom(Codex c)
        {
            Title = c.Title;
            SortingTitle = c.UserDefinedSortingTitle; //copy field instead of property, or it will copy _title
            Sources = c.Sources.Copy();
            Authors = new(c.Authors);
            Publisher = c.Publisher;
            Version = c.Version;
            Id = c.Id;
            CoverArtPath = c.CoverArtPath;
            ThumbnailPath = c.ThumbnailPath;
            PhysicallyOwned = c.PhysicallyOwned;
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

        public Codex Clone()
        {
            return new(this);
        }

        public void ClearPersonalData()
        {
            Favorite = false;
            PhysicallyOwned = false;
            DateAdded = DateTime.Now;
            OpenedCount = 0;
            LastOpened = default;
            Rating = 0;
        }
        
        public void NotifyCoverChanged() => CoverChanged?.Invoke(this, EventArgs.Empty);
        
        #endregion

        #region Events

        public event EventHandler<EventArgs>? CoverChanged;

        #endregion
    }
}

