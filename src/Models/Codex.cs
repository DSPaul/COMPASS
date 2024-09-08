using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Models.CodexProperties;
using COMPASS.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace COMPASS.Models
{
    public class Codex : ObservableObject, IHasID
    {
        #region Constructors

        public Codex()
        {
            Authors.CollectionChanged += OnCollectionChanged;
            Tags.CollectionChanged += OnCollectionChanged;
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

        #endregion

        public void SetImagePaths(CodexCollection collection)
        {
            CoverArt = System.IO.Path.Combine(collection.CoverArtPath, $"{ID}.png");
            Thumbnail = System.IO.Path.Combine(collection.ThumbnailsPath, $"{ID}.png");
        }

        #region Properties

        #region COMPASS related Metadata

        public int ID { get; set; }

        private string _coverArt = "";
        public string CoverArt
        {
            get => _coverArt;
            set => SetProperty(ref _coverArt, value);
        }

        private string _thumbnail = "";
        public string Thumbnail
        {
            get => _thumbnail;
            set => SetProperty(ref _thumbnail, value);
        }

        #endregion

        #region Codex related Metadata

        private string _title = "";
        public string Title
        {
            get => _title;
            set
            {
                if (value is null) return;
                SetProperty(ref _title, value);
                OnPropertyChanged(nameof(SortingTitle));
                OnPropertyChanged(nameof(SortingTitleContainsNumbers));
            }
        }

        private string _userDefinedSortingTitle = "";
        /// <summary>
        /// Sorting title defined by the user, will only have a value if it is different from the title
        /// </summary>
        public string UserDefinedSortingTitle => _userDefinedSortingTitle;
        public string SortingTitle
        {
            get => (String.IsNullOrEmpty(_userDefinedSortingTitle) ? _title : _userDefinedSortingTitle).PadNumbers();
            set
            {
                SetProperty(ref _userDefinedSortingTitle, value);
                OnPropertyChanged(nameof(SortingTitleContainsNumbers));
            }
        }
        public bool SortingTitleContainsNumbers => Constants.RegexNumbersOnly().IsMatch(SortingTitle);
        public string ZeroPaddingExplainer =>
            "What's with all the 0's? \n \n" +
            "Zero-padding numbers ensures numerical sorting instead of alphabetical sorting. \n" +
            "Consider the numbers 1, 2, 13, and 20. \n" +
            "Without zero-padding, they would be sorted alphabetically as 1, 13, 2, 20. \n" +
            "However, with zero-padding, the order becomes 01, 02, 13, 20. \n";

        private ObservableCollection<string> _authors = new();
        public ObservableCollection<string> Authors
        {
            get => _authors;
            set
            {
                _authors.CollectionChanged -= OnCollectionChanged;
                SetProperty(ref _authors, value);
                _authors.CollectionChanged += OnCollectionChanged;
                OnPropertyChanged(nameof(AuthorsAsString));
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

        private ObservableCollection<Tag> _tags = new();
        public ObservableCollection<Tag> Tags
        {
            get => _tags;
            set
            {
                _tags.CollectionChanged -= OnCollectionChanged;
                _tags = value;
                _tags.CollectionChanged += OnCollectionChanged;
                OnPropertyChanged(nameof(OrderedTags));
            }
        }

        //order them in same order as alltags by starting with alltags and keeping the ones we need using intersect
        public IEnumerable<Tag> OrderedTags => _tags.FirstOrDefault()?.AllTags.Intersect(_tags) ?? Enumerable.Empty<Tag>();

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

        #region Sources
        private string _sourceURL = "";
        public string SourceURL
        {
            get => _sourceURL;
            set
            {
                if (value.StartsWith("www."))
                {
                    value = @"https://" + value;
                }
                SetProperty(ref _sourceURL, value);
            }
        }

        private string _path = "";
        public string Path
        {
            get => _path;
            set => SetProperty(ref _path, value);
        }

        private string _isbn = "";
        public string ISBN
        {
            get => _isbn;
            set => SetProperty(ref _isbn, value);
        }
        #endregion

        public string? FileType
        {
            get
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
        }
        public string FileName => System.IO.Path.GetFileName(Path);
        #endregion

        #region Methods
        public void Copy(Codex c)
        {
            Title = c.Title;
            SortingTitle = c.UserDefinedSortingTitle; //copy field instead of property, or it will copy _title
            Path = c.Path;
            Authors = new(c.Authors);
            Publisher = c.Publisher;
            Version = c.Version;
            SourceURL = c.SourceURL;
            ID = c.ID;
            CoverArt = c.CoverArt;
            Thumbnail = c.Thumbnail;
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
            ISBN = c.ISBN;
        }

        public void RefreshThumbnail() => OnPropertyChanged(nameof(Thumbnail));

        public void ClearPersonalData()
        {
            Favorite = false;
            PhysicallyOwned = false;
            DateAdded = DateTime.Now;
            OpenedCount = 0;
            LastOpened = default;
            Rating = 0;
        }

        private void OnCollectionChanged(object? o, NotifyCollectionChangedEventArgs args)
        {
            if (o == Tags) OnPropertyChanged(nameof(OrderedTags));
            if (o == Authors) OnPropertyChanged(nameof(AuthorsAsString));
        }
        #endregion
        public bool HasOfflineSource() => !String.IsNullOrWhiteSpace(Path);

        public bool HasOnlineSource() => !String.IsNullOrWhiteSpace(SourceURL);

        public static readonly List<CodexProperty> MedataProperties = new()
        {
            CodexProperty.GetInstance(nameof(Title))!,
            CodexProperty.GetInstance(nameof(Authors))!,
            CodexProperty.GetInstance(nameof(Publisher))!,
            CodexProperty.GetInstance(nameof(Version))!,
            CodexProperty.GetInstance(nameof(PageCount))!,
            CodexProperty.GetInstance(nameof(Tags))!,
            CodexProperty.GetInstance(nameof(Description))!,
            CodexProperty.GetInstance(nameof(ReleaseDate))!,
            CodexProperty.GetInstance(nameof(CoverArt))!,
        };
    }
}

