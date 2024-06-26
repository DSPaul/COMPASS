﻿using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Services;
using COMPASS.Tools;
using COMPASS.ViewModels.Sources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace COMPASS.Models
{
    public class Codex : ObservableObject, IHasID
    {
        public Codex()
        {
            Authors.CollectionChanged += (_, _) => OnPropertyChanged(nameof(AuthorsAsString));
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

        public void SetImagePaths(CodexCollection collection)
        {
            CoverArt = System.IO.Path.Combine(collection.CoverArtPath, $"{ID}.png");
            Thumbnail = System.IO.Path.Combine(collection.ThumbnailsPath, $"{ID}.png");
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

        #region Properties

        private string _path = "";
        public string Path
        {
            get => _path;
            set
            {
                value = IOService.SanitizeXmlString(value);
                SetProperty(ref _path, value);
            }
        }

        private string _title = "";
        public string Title
        {
            get => _title;
            set
            {
                if (value is null) return;
                value = IOService.SanitizeXmlString(value);
                SetProperty(ref _title, value);
                OnPropertyChanged(nameof(SortingTitle));
                OnPropertyChanged(nameof(SortingTitleContainsNumbers));
            }
        }

        private string _sortingTitle = "";
        [XmlIgnore]
        public string SortingTitle
        {
            get => (String.IsNullOrEmpty(_sortingTitle) ? _title : _sortingTitle).PadNumbers();
            set
            {
                SetProperty(ref _sortingTitle, value);
                OnPropertyChanged(nameof(SortingTitleContainsNumbers));
            }
        }
        //separate property needed for serialization or it will get _title and save that
        //instead of saving an empty and mirroring _title during runtime
        public string SerializableSortingTitle
        {
            get => _sortingTitle;
            set
            {
                value = IOService.SanitizeXmlString(value);
                SetProperty(ref _sortingTitle, value);
            }
        }

        [XmlIgnore]
        public bool SortingTitleContainsNumbers => Constants.RegexNumbersOnly().IsMatch(SortingTitle);
        [XmlIgnore]
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
                SetProperty(ref _authors, value);
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
            set
            {
                value = IOService.SanitizeXmlString(value);
                SetProperty(ref _publisher, value);
            }
        }

        private string _version = "";
        public string Version
        {
            get => _version;
            set
            {
                value = IOService.SanitizeXmlString(value);
                SetProperty(ref _version, value);
            }
        }

        private string _sourceURL = "";
        public string SourceURL
        {
            get => _sourceURL;
            set
            {
                value = IOService.SanitizeXmlString(value);
                SetProperty(ref _sourceURL, value);
            }
        }

        public int ID { get; set; }

        private string _coverArt = "";
        public string CoverArt
        {
            get => _coverArt;
            set
            {
                value = IOService.SanitizeXmlString(value);
                SetProperty(ref _coverArt, value);
            }
        }

        private string _thumbnail = "";
        public string Thumbnail
        {
            get => _thumbnail;
            set
            {
                value = IOService.SanitizeXmlString(value);
                SetProperty(ref _thumbnail, value);
            }
        }

        private bool _physicallyOwned;
        public bool PhysicallyOwned
        {
            get => _physicallyOwned;
            set => SetProperty(ref _physicallyOwned, value);
        }

        private ObservableCollection<Tag> _tags = new();
        //Don't save all the tags, only save ID's instead
        [XmlIgnore]
        public ObservableCollection<Tag> Tags
        {
            get
            {
                App.SafeDispatcher.Invoke(() =>
                {
                    //order them in same order as alltags by starting with alltags and keeping the ones we need using intersect
                    List<Tag> orderedTags = _tags.FirstOrDefault()?.AllTags.Intersect(_tags).ToList() ?? new List<Tag>();
                    _tags.Clear(); //will fail when called from non UI thread which happens during import
                    _tags.AddRange(orderedTags);
                });
                return _tags;
            }

            set => SetProperty(ref _tags, value);
        }
        public List<int> TagIDs { get; set; } = new();

        private string _description = "";
        public string Description
        {
            get => _description;
            set
            {
                value = IOService.SanitizeXmlString(value);
                SetProperty(ref _description, value);
            }
        }

        private DateTime? _releaseDate;
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

        private int _openedCount;
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

        private string _isbn = "";
        public string ISBN
        {
            get => _isbn;
            set
            {
                value = IOService.SanitizeXmlString(value);
                SetProperty(ref _isbn, value);
            }
        }

        public bool HasOfflineSource() => !String.IsNullOrWhiteSpace(Path);

        public bool HasOnlineSource() => !String.IsNullOrWhiteSpace(SourceURL);

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

        public static readonly List<CodexProperty> Properties = new()
        {
            new(nameof(Title),
                isEmpty: codex => String.IsNullOrWhiteSpace(codex.Title),
                setProp: (codex,other) => codex.Title = other.Title,
                defaultSources: new()
                {
                    MetaDataSource.PDF,
                    MetaDataSource.File,
                    MetaDataSource.GmBinder,
                    MetaDataSource.Homebrewery,
                    MetaDataSource.GoogleDrive,
                    MetaDataSource.ISBN,
                    MetaDataSource.GenericURL
                }),
            new( nameof(Authors),
                isEmpty: codex => codex.Authors is null || !codex.Authors.Any(),
                setProp: (codex,other) => codex.Authors = other.Authors,
                defaultSources: new()
                {
                    MetaDataSource.PDF,
                    MetaDataSource.GmBinder,
                    MetaDataSource.Homebrewery,
                    MetaDataSource.ISBN,
                    MetaDataSource.GenericURL
                }),
            new( nameof(Publisher),
                isEmpty: codex => String.IsNullOrEmpty(codex.Publisher),
                setProp: (codex,other) => codex.Publisher = other.Publisher,
                defaultSources: new()
                {
                    MetaDataSource.ISBN,
                    MetaDataSource.GmBinder,
                    MetaDataSource.Homebrewery,
                    MetaDataSource.GoogleDrive,
                }),
            new( nameof(Version),
                isEmpty: codex => String.IsNullOrEmpty(codex.Version),
                setProp: (codex,other) => codex.Version = other.Version,
                defaultSources: new()
                {
                    MetaDataSource.Homebrewery
                }),
            new( nameof(PageCount),
                isEmpty: codex => codex.PageCount == 0,
                setProp: (codex, other) => codex.PageCount = other.PageCount,
                defaultSources: new()
                {
                    MetaDataSource.PDF,
                    MetaDataSource.Image,
                    MetaDataSource.GmBinder,
                    MetaDataSource.Homebrewery,
                    MetaDataSource.ISBN,
                },
                label: "Pagecount"),
            new( nameof(Tags),
                isEmpty: codex => codex.Tags is null || !codex.Tags.Any(),
                setProp: (codex,other) =>
                {
                    foreach (var tag in other.Tags)
                    {
                        App.SafeDispatcher.Invoke(() => codex.Tags.AddIfMissing(tag));
                    }
                },
                defaultSources : new()
                {
                    MetaDataSource.File,
                    MetaDataSource.GenericURL,
                }),
            new( nameof(Description),
                codex => String.IsNullOrEmpty(codex.Description),
                (codex,other) => codex.Description = other.Description,
                defaultSources : new()
                {
                    MetaDataSource.Homebrewery,
                    MetaDataSource.ISBN,
                    MetaDataSource.GenericURL,
                }),
            new( nameof(ReleaseDate),
                isEmpty: codex => codex.ReleaseDate is null || codex.ReleaseDate == DateTime.MinValue,
                setProp: (codex, other) => codex.ReleaseDate = other.ReleaseDate,
                defaultSources : new()
                {
                    MetaDataSource.Homebrewery,
                    MetaDataSource.ISBN,
                },
                label: "Release Date"),
            new( nameof(CoverArt),
                isEmpty: codex => !File.Exists(codex.CoverArt),
                setProp: (codex,other) =>
                {
                    codex.CoverArt = other.CoverArt;
                    codex.Thumbnail = other.Thumbnail;
                },
                defaultSources : new()
                {
                    MetaDataSource.Image,
                    MetaDataSource.PDF,
                    MetaDataSource.GmBinder,
                    MetaDataSource.Homebrewery,
                    MetaDataSource.GoogleDrive,
                    MetaDataSource.ISBN,
                },
                label: "Cover Art"),
        };
    }
}

