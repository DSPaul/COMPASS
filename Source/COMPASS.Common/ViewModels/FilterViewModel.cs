﻿using Avalonia.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Models;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Models.Filters;
using COMPASS.Common.Services;
using COMPASS.Common.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace COMPASS.Common.ViewModels
{
    public class FilterViewModel : ViewModelBase
    {
        public FilterViewModel(ObservableCollection<Codex> allCodices)
        {
            _allCodices = allCodices;

            // Load sorting from settings
            InitSortingProperties();

            _includedCodices = [.._allCodices];
            _excludedCodices = [];

            IncludedFilters.CollectionChanged += (_, _) => UpdateIncludedCodices();
            ExcludedFilters.CollectionChanged += (_, _) => UpdateExcludedCodices();

            _allCodices.CollectionChanged += (_, _) => SubscribeToCodexProperties();
            SubscribeToCodexProperties();

            PopulateMetaDataCollections();

            ApplyFilters();
        }

        #region Fields

        PreferencesService _preferencesService = PreferencesService.GetInstance();

        private ObservableCollection<Codex> _allCodices;
        private readonly int _itemsShown = 15;
        public int ItemsShown => Math.Min(_itemsShown, FilteredCodices?.Count ?? 0);


        private HashSet<Codex> _includedCodices;
        private HashSet<Codex> _excludedCodices;

        #endregion

        #region Properties

        private bool _include = true;
        public bool Include
        {
            get => _include;
            set => SetProperty(ref _include, value);
        }

        public ObservableCollection<Filter> IncludedFilters { get; set; } = [];
        public ObservableCollection<Filter> ExcludedFilters { get; set; } = [];
        public bool HasActiveFilters => IncludedFilters.Any() || ExcludedFilters.Any();


        private ObservableCollection<Codex>? _filteredCodices;
        public ObservableCollection<Codex>? FilteredCodices
        {
            get => _filteredCodices;
            set => SetProperty(ref _filteredCodices, value);
        }

        public ObservableCollection<Codex> Favorites => FilteredCodices is null ? new() :
            new(FilteredCodices.Where(c => c.Favorite));
        public List<Codex> RecentCodices => FilteredCodices is null ? []
            :
            FilteredCodices.OrderByDescending(c => c.LastOpened).ToList().GetRange(0, ItemsShown);
        public List<Codex> MostOpenedCodices => FilteredCodices is null ? []
            :
            FilteredCodices.OrderByDescending(c => c.OpenedCount).ToList().GetRange(0, ItemsShown);
        public List<Codex> RecentlyAddedCodices => FilteredCodices is null ? []
            :
            FilteredCodices.OrderByDescending(c => c.DateAdded).ToList().GetRange(0, ItemsShown);

        private string _searchTerm = "";
        public string SearchTerm
        {
            get => _searchTerm;
            set => SetProperty(ref _searchTerm, value);
        }

        public List<Filter> BooleanFilters { get; } =
        [
            new OfflineSourceFilter(),
            new OnlineSourceFilter(),
            new PhysicalSourceFilter(),
            new FavoriteFilter()
        ];

        #region Clear selection on comboboxes
        
        private string? _noneSelection;
        public string? NoneSelection
        {
            get => _noneSelection;
            set => SetProperty(ref _noneSelection, value);
        }

        private void ClearSelelection()
        {
            //Just setting it to null doesnt work due to one way binding, 
            //value actually needs to change
            NoneSelection = "";
            NoneSelection = null;
        }
        
        #endregion
        
        public string SelectedAuthor
        {
            set
            {
                if (String.IsNullOrEmpty(value)) return;
                Filter authorFilter = new AuthorFilter(value);
                AddFilter(authorFilter, Include);
                ClearSelelection();
            }
        }

        private ObservableCollection<string> _authorList = [];
        public ObservableCollection<string> AuthorList
        {
            get => _authorList;
            set => SetProperty(ref _authorList, value);
        }

        public string SelectedPublisher
        {
            set
            {
                if (String.IsNullOrEmpty(value)) return;
                Filter publisherFilter = new PublisherFilter(value);
                AddFilter(publisherFilter, Include);
                ClearSelelection();
            }
        }

        private ObservableCollection<string> _publisherList = [];
        public ObservableCollection<string> PublisherList
        {
            get => _publisherList;
            set => SetProperty(ref _publisherList, value);
        }

        public string SelectedFileType
        {
            set
            {
                if (String.IsNullOrEmpty(value)) return;
                Filter fileExtensionFilter = new FileExtensionFilter(value);
                AddFilter(fileExtensionFilter, Include);
                ClearSelelection();
            }
        }
        private ObservableCollection<string> _fileTypeList = [];
        public ObservableCollection<string> FileTypeList
        {
            get => _fileTypeList;
            set => SetProperty(ref _fileTypeList, value);
        }

        public string SelectedDomain
        {
            set
            {
                if (String.IsNullOrEmpty(value)) return;
                Filter domainFilter = new DomainFilter(value);
                AddFilter(domainFilter, Include);
                ClearSelelection();
            }
        }
        private ObservableCollection<string> _domainList = [];
        public ObservableCollection<string> DomainList
        {
            get => _domainList;
            set => SetProperty(ref _domainList, value);
        }

        public CodexProperty SelectedNotEmptyProperty
        {
            set
            {
                if (value is not null)
                {
                    Filter notEmptyFilter = new NotEmptyFilter(value);
                    AddFilter(notEmptyFilter, Include);
                    ClearSelelection();
                }
            }
        }

        public static List<CodexProperty> PossibleEmptyProperties { get; } =
        [
            CodexProperty.GetInstance(nameof(Codex.Authors))!,
            CodexProperty.GetInstance(nameof(Codex.CoverArtPath))!,
            CodexProperty.GetInstance(nameof(Codex.Description))!,
            CodexProperty.GetInstance(nameof(Codex.Sources.ISBN))!,
            CodexProperty.GetInstance(nameof(Codex.PageCount))!,
            CodexProperty.GetInstance(nameof(Codex.Publisher))!,
            CodexProperty.GetInstance(nameof(Codex.Rating))!,
            CodexProperty.GetInstance(nameof(Codex.ReleaseDate))!,
            CodexProperty.GetInstance(nameof(Codex.Tags))!,
            CodexProperty.GetInstance(nameof(Codex.Title))!,
            CodexProperty.GetInstance(nameof(Codex.Version))!
        ];

        //Selected Start and Stop Release Dates
        private DateTime? _startReleaseDate;
        private DateTime? _stopReleaseDate;

        public DateTime? StartReleaseDate
        {
            get => _startReleaseDate;
            set
            {
                SetProperty(ref _startReleaseDate, value);
                if (value is null) return;
                Filter startDateFilter = new StartReleaseDateFilter(value.Value);
                AddFilter(startDateFilter, Include);
            }
        }

        public DateTime? StopReleaseDate
        {
            get => _stopReleaseDate;
            set
            {
                SetProperty(ref _stopReleaseDate, value);
                if (value != null)
                {
                    Filter stopDateFilter = new StopReleaseDateFilter(value.Value);
                    AddFilter(stopDateFilter, Include);
                }
            }
        }

        //Selected minimum rating
        private int _minRating;
        public int MinRating
        {
            get => _minRating;
            set
            {
                SetProperty(ref _minRating, value);
                if (value is > 0 and < 6)
                {
                    Filter minRatFilter = new MinimumRatingFilter(value);
                    AddFilter(minRatFilter, Include);
                }
            }
        }

        public bool SortAscending
        {
            get => SortDirection == ListSortDirection.Ascending;
            set => SortDirection = value ? ListSortDirection.Ascending : ListSortDirection.Descending;
        }
        
        public ListSortDirection SortDirection
        {
            get => _preferencesService.Preferences.UIState.SortDirection;
            set
            {
                if (value != _preferencesService.Preferences.UIState.SortDirection)
                {
                    _preferencesService.Preferences.UIState.SortDirection = value;
                    ApplySorting();
                    OnPropertyChanged();
                }
            }
        }

        public string SortProperty
        {
            get => _preferencesService.Preferences.UIState.SortProperty;
            set
            {
                if (!string.IsNullOrEmpty(value) && _preferencesService.Preferences.UIState.SortProperty != value)
                {
                    _preferencesService.Preferences.UIState.SortProperty = value;
                    ApplySorting();
                    OnPropertyChanged();
                }
            }
        }

        public Dictionary<string, string> SortOptions { get; } = new()
        {
            //("Display name","Property Name")
            { "Title", "SortingTitle" },
            { "Author", "AuthorsAsString" },
            { "Publisher", "Publisher" },
            { "User Rating", "Rating" },
            { "Date - Released", "ReleaseDate" },
            { "Date - Last Opened", "LastOpened"},
            { "Date - Added", "DateAdded" },
            { "Page Count", "PageCount" },
            { "Times opened", "OpenedCount" }
        };

        #endregion

        #region Methods and Commands
        private void SubscribeToCodexProperties()
        {
            //cause derived lists to update when codex gets updated
            foreach (Codex c in _allCodices)
            {
                c.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Favorites));
                c.PropertyChanged += (_, _) => OnPropertyChanged(nameof(RecentCodices));
                c.PropertyChanged += (_, _) => OnPropertyChanged(nameof(MostOpenedCodices));
            }
        }

        private void InitSortingProperties()
        {
            //double check on typos by checking if all property names exist in codex class
            var possibleSortPropertyNames = typeof(Codex).GetProperties().Select(p => p.Name).ToList();
            if (SortOptions.Select(pair => pair.Value).Except(possibleSortPropertyNames).Any())
            {
                Logger.Warn("One of the sort property paths does not exist", new MissingMemberException());
            }
        }

        public void PopulateMetaDataCollections() => Dispatcher.UIThread.Invoke(() =>
        {
            foreach (Codex c in _allCodices)
            {
                //Populate Author Collection
                AuthorList = new(AuthorList.Union(c.Authors));

                //Populate Publisher Collection
                if (!String.IsNullOrEmpty(c.Publisher)) PublisherList.AddIfMissing(c.Publisher);

                //Populate FileType Collection
                if (!String.IsNullOrEmpty(c.Sources.FileType)) FileTypeList.AddIfMissing(c.Sources.FileType);

                //Populate Domain Collection
                if (c.Sources.HasOnlineSource())
                {
                    string domain = Uri.IsWellFormedUriString(c.Sources.SourceURL, UriKind.Absolute) ?
                        new Uri(c.Sources.SourceURL).Host :
                        c.Sources.SourceURL;
                    if (!string.IsNullOrEmpty(domain)) DomainList.AddIfMissing(domain);
                }
            }
            AuthorList.Remove(""); //remove "" author because String.IsNullOrEmpty cannot be called during Union

            //Sort them
            AuthorList = new(AuthorList.Order());
            PublisherList = new(PublisherList.Order());
            FileTypeList = new(FileTypeList.Order());
            DomainList = new(DomainList.Order());
        });

        //------------- Adding, Removing, ect ------------//

        // Remove Filter
        private RelayCommand<Filter>? _removeFromItemsControlCommand;
        public RelayCommand<Filter> RemoveFromItemsControlCommand => _removeFromItemsControlCommand ??= new(RemoveFilter);
        public void RemoveFilter(Filter? filter)
        {
            if (filter is null) return;
            IncludedFilters.Remove(filter);
            ExcludedFilters.Remove(filter);
        }
        public void RemoveFilterType(FilterType filterType)
        {
            IncludedFilters.RemoveWhere(filter => filter.Type == filterType);
            ExcludedFilters.RemoveWhere(filter => filter.Type == filterType);
        }

        // Add Filter
        private RelayCommand<Filter>? _addSourceFilterCommand;
        public RelayCommand<Filter> AddSourceFilterCommand => _addSourceFilterCommand ??= new(AddSourceFilter);
        public void AddSourceFilter(Filter? filter) => AddFilter(filter, Include);
        public void AddFilter(Filter? filter, bool include = true)
        {
            if (filter is null) return;

            //TODO: check how keyboard shortcuts are done in avalonia
            //if (Keyboard.Modifiers == ModifierKeys.Alt)
            //{
            //    include = false;
            //}

            ObservableCollection<Filter> target = include ? IncludedFilters : ExcludedFilters;
            ObservableCollection<Filter> other = !include ? IncludedFilters : ExcludedFilters;

            //if Filter does not allow multiple instances, remove previous instance(s) of that Filter before adding
            if (!filter.AllowMultiple && target.Any(f => f.Type == filter.Type))
            {
                target.RemoveWhere(f => f.Type == filter.Type);
            }

            target.AddIfMissing(filter);
            other.Remove(filter); //filter should never occur in both include and exclude so remove from other
        }

        private RelayCommand<string>? _searchCommand;
        public RelayCommand<string> SearchCommand => _searchCommand ??= new(SearchCommandHelper);
        private void SearchCommandHelper(string? searchTerm)
        {
            if (!String.IsNullOrEmpty(searchTerm))
            {
                Filter searchFilter = new SearchFilter(searchTerm);
                AddFilter(searchFilter);
            }
            else
            {
                RemoveFilterType(FilterType.Search);
            }
        }

        //Clear Filters
        private RelayCommand? _clearFiltersCommand;
        public RelayCommand ClearFiltersCommand => _clearFiltersCommand ??= new(ClearFilters);
        public void ClearFilters()
        {
            StartReleaseDate = null;
            StopReleaseDate = null;
            MinRating = 0;
            SearchTerm = "";

            IncludedFilters.Clear();
            ExcludedFilters.Clear();
        }


        //------------- Filter Logic ------------//
        private void UpdateIncludedCodices(bool apply = true)
        {
            _includedCodices = [.._allCodices];
            foreach (FilterType filterType in Enum.GetValues(typeof(FilterType)))
            {
                // Included codices must match filters of all types so IntersectWith()
                _includedCodices.IntersectWith(GetFilteredCodicesByType(IncludedFilters, filterType, true));
            }
            if (apply) ApplyFilters();
        }
        private void UpdateExcludedCodices(bool apply = true)
        {
            _excludedCodices = [];
            foreach (FilterType filterType in Enum.GetValues(typeof(FilterType)))
            {
                // Codex is excluded as soon as it matches any excluded filter so UnionWith()
                _excludedCodices.UnionWith(GetFilteredCodicesByType(ExcludedFilters, filterType, false));
            }
            if (apply) ApplyFilters();
        }

        /// <summary>
        /// Get list of Codices that match filters of one filter type
        /// </summary>
        /// <param name="filters"></param>
        /// <param name="filterType"></param>
        /// <param name="include"> Determines whether returned codices should be included or excluded </param>
        /// <returns></returns>
        private IEnumerable<Codex> GetFilteredCodicesByType(IEnumerable<Filter> filters, FilterType filterType, bool include)
        {
            List<Filter> relevantFilters = [..filters.Where(filter => filter.Type == filterType)];

            if (relevantFilters.Count == 0) return include ? _allCodices : Enumerable.Empty<Codex>();

            return filterType switch
            {
                FilterType.Tag => GetFilteredCodicesByTags(relevantFilters, include),
                _ => _allCodices.Where(codex => relevantFilters.Any(filter => filter.Apply(codex)))
            };
        }

        private HashSet<Codex> GetFilteredCodicesByTags(IEnumerable<Filter> filters, bool include)
            => include ? GetIncludedCodicesByTags(filters) : GetExcludedCodicesByTags(filters);
        private HashSet<Codex> GetIncludedCodicesByTags(IEnumerable<Filter> filters)
        {
            HashSet<Codex> includedCodices = [.._allCodices];

            List<Tag> includedTags = filters
                .Select(filter => (Tag)filter.FilterValue!)
                .ToList();

            if (includedTags.Count > 0)
            {
                HashSet<Tag> includedGroups = includedTags.Select(tag => tag.GetGroup()).ToHashSet();

                // Go over every group, tags within same group have OR relation, groups have AND relation
                foreach (Tag group in includedGroups)
                {
                    // Make list with all included tags in that group, including children
                    List<Tag> singleGroupTags = includedTags.Where(tag => tag.GetGroup() == group).Flatten().ToList();
                    // Add parents of those tags, must come AFTER children, otherwise children of parents are included which is wrong
                    for (int i = 0; i < singleGroupTags.Count; i++)
                    {
                        Tag? parentTag = singleGroupTags[i].Parent;
                        if (parentTag is not null && !parentTag.IsGroup) singleGroupTags.AddIfMissing(parentTag);
                    }

                    //List of codices that match filters in one group
                    HashSet<Codex> singleGroupFilteredCodices =
                        [.._allCodices.Where(codex => singleGroupTags.Intersect(codex.Tags).Any())];

                    includedCodices = includedCodices.Intersect(singleGroupFilteredCodices).ToHashSet();
                }
            }
            return includedCodices;
        }
        private HashSet<Codex> GetExcludedCodicesByTags(IEnumerable<Filter> filters)
        {
            HashSet<Codex> excludedCodices = [];

            var excludedTags = filters.Select(filter => (Tag)filter.FilterValue!).ToList();

            if (excludedTags.Count > 0)
            {
                // If parent is excluded, so should all the children
                excludedTags = excludedTags.Flatten().ToList();
                excludedCodices = [.._allCodices.Where(c => excludedTags.Intersect(c.Tags).Any())];
            }

            return excludedCodices;
        }
        //------------------------------------//

        private void ApplySorting()
        {
            FilteredCodices?.Sort(c => c.GetPropertyValue(SortProperty), SortDirection);
        }
        
        private void ApplyFilters(bool force = false)
        {
            IList<Codex> filteredCodices = _allCodices
                .Intersect(_includedCodices)
                .Except(_excludedCodices)
                .ToList();

            if (force || FilteredCodices is null || !FilteredCodices.SequenceEqual(filteredCodices))
            {
                FilteredCodices = new(filteredCodices);
                //Also apply filtering to these lists
                OnPropertyChanged(nameof(Favorites));
                OnPropertyChanged(nameof(RecentCodices));
                OnPropertyChanged(nameof(MostOpenedCodices));
                OnPropertyChanged(nameof(RecentlyAddedCodices));
            }
            ApplySorting();
        }

        public void ReFilter(bool force = false)
        {
            try
            {
                UpdateIncludedCodices(false);
                UpdateExcludedCodices(false);
                ApplyFilters(force);
            }
            catch (Exception ex)
            {
                Logger.Warn("Something when wrong during filtering", ex);
            }
        }

        public void RemoveCodex(Codex c)
        {
            _excludedCodices.Remove(c);

            //Changes to CollectionView Should always be done from dispatcher thread
            Dispatcher.UIThread.Invoke(() => FilteredCodices?.Remove(c));
        }

        #endregion

        #region Drag Drop Handlers
        //Drop on Treeview Behaviour
        void OnDragOver(object sender, DragEventArgs e)
        {
            //Move From Treeview
            if (e.Data.GetValue<TreeNode>() is { Tag.IsGroup: false })
            {
                e.DragEffects = DragDropEffects.Copy;
            }
            //Move Filter to included/excluded
            else if (e.Data.GetValue<Filter>() != null)
            {
                e.DragEffects = DragDropEffects.Move;
            }
            //Move Tag between included/excluded
            else if (e.Data.GetValue<Tag>() != null)
            {
                e.DragEffects = DragDropEffects.Move;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
        }

        void OnDrop(object sender, DragEventArgs e)
        {
            //Included filter Listbox has extra empty collection to tell them apart
            //TODO: get TargetCollection from sender somehow
            //OLD CODE: bool toIncluded = ((CompositeCollection)dropInfo.TargetCollection).Count > 1;
            bool toIncluded = false;

            //Move From Treeview
            if (e.Data.GetValue<TreeNode>() is { Tag.IsGroup: false } node)
            {
                AddFilter(new TagFilter(node.Tag), toIncluded);
            }
            //Move Filter to included/excluded
            else if (e.Data.GetValue<Filter>() is Filter draggedFilter)
            {
                AddFilter(draggedFilter, toIncluded);
            }
            //Move Tag between included/excluded
            else if (e.Data.GetValue<Tag>() is Tag draggedTag)
            {
                AddFilter(new TagFilter(draggedTag), toIncluded);
            }
        }
        #endregion
    }
}
