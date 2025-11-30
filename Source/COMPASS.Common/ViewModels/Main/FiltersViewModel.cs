using Avalonia.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Models;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Models.Filters;
using COMPASS.Common.Models.Hierarchy;
using COMPASS.Common.Services;
using COMPASS.Common.Tools;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Infra.ExtensionMethods;

namespace COMPASS.Common.ViewModels.Main
{
    public class FiltersViewModel : ViewModelBase
    {
        public FiltersViewModel(ObservableCollection<CodexViewModel> allCodexVms, FiltersState? filtersState = null)
        {
            _allCodexVms = allCodexVms;

            // Load sorting from settings
            InitSortingProperties();

            _includedCodices = [.. _allCodexVms];
            _excludedCodices = [];

            if (filtersState != null)
            {
                if (filtersState.IncludedFilters.Any())
                {
                    IncludedFilters = new(filtersState.IncludedFilters.Select(ModelVmFactory.GetFilterViewModel));
                    UpdateIncludedCodices(false);
                }
                if (filtersState.ExcludedFilters.Any())
                {
                    ExcludedFilters = new(filtersState.ExcludedFilters.Select(ModelVmFactory.GetFilterViewModel));
                    UpdateExcludedCodices(false);
                }
            }

            IncludedFilters.CollectionChanged += (_, _) => UpdateIncludedCodices();
            ExcludedFilters.CollectionChanged += (_, _) => UpdateExcludedCodices();

            _allCodexVms.CollectionChanged += OnCodexCollectionChanged;
            SubscribeToCodexProperties(_allCodexVms);

            PopulateMetaDataCollections();

            ReFilter();
        }

        #region Fields

        private readonly PreferencesService _preferencesService = PreferencesService.GetInstance();
        
        private readonly ObservableCollection<CodexViewModel> _allCodexVms;
        private readonly int _itemsShown = 15;
        public int ItemsShown => Math.Min(_itemsShown, FilteredCodices?.Count ?? 0);
        
        private HashSet<CodexViewModel> _includedCodices;
        private HashSet<CodexViewModel> _excludedCodices;

        #endregion

        #region Properties

        private bool _include = true;
        public bool Include
        {
            get => _include;
            set => SetProperty(ref _include, value);
        }

        public ObservableCollection<FilterViewModel> IncludedFilters { get; set; } = [];
        public ObservableCollection<FilterViewModel> ExcludedFilters { get; set; } = [];
        public bool HasActiveFilters => IncludedFilters.Any() || ExcludedFilters.Any();

        public ObservableCollection<CodexViewModel> FilteredCodices { get; } = [];

        public ObservableCollection<CodexViewModel> Favorites => new(FilteredCodices.Where(c => c.Favorite));
        public List<CodexViewModel> RecentCodices => FilteredCodices.OrderByDescending(c => c.LastOpened).ToList().GetRange(0, ItemsShown);
        public List<CodexViewModel> MostOpenedCodices => FilteredCodices.OrderByDescending(c => c.OpenedCount).ToList().GetRange(0, ItemsShown);
        public List<CodexViewModel> RecentlyAddedCodices => FilteredCodices.OrderByDescending(c => c.DateAdded).ToList().GetRange(0, ItemsShown);

        private string _searchTerm = "";
        public string SearchTerm
        {
            get => _searchTerm;
            set => SetProperty(ref _searchTerm, value);
        }

        
        private List<Filter> _booleanFilters =
        [
            new OfflineSourceFilter(),
            new OnlineSourceFilter(),
            new PhysicalSourceFilter(),
            new FavoriteFilter()
        ];

        public List<FilterViewModel> BooleanFilters => field ??= _booleanFilters.Select(ModelVmFactory.GetFilterViewModel).ToList();

        #region Clear selection on comboboxes

        private string? _noneSelection;
        public string? NoneSelection
        {
            get => _noneSelection;
            set => SetProperty(ref _noneSelection, value);
        }

        private void ClearSelection()
        {
            //Just setting it to null doesn't work due to one way binding, 
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
                ActivateFilter(authorFilter, Include);
                ClearSelection();
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
                ActivateFilter(publisherFilter, Include);
                ClearSelection();
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
                ActivateFilter(fileExtensionFilter, Include);
                ClearSelection();
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
                ActivateFilter(domainFilter, Include);
                ClearSelection();
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
                    ActivateFilter(notEmptyFilter, Include);
                    ClearSelection();
                }
            }
        }

        public static List<CodexProperty> PossibleEmptyProperties { get; } =
        [
            CodexProperty.GetInstance(nameof(CodexViewModel.Authors))!,
            CodexProperty.GetInstance(nameof(CodexViewModel.Cover))!,
            CodexProperty.GetInstance(nameof(CodexViewModel.Description))!,
            CodexProperty.GetInstance(nameof(CodexViewModel.Sources.ISBN))!,
            CodexProperty.GetInstance(nameof(CodexViewModel.PageCount))!,
            CodexProperty.GetInstance(nameof(CodexViewModel.Publisher))!,
            CodexProperty.GetInstance(nameof(CodexViewModel.Rating))!,
            CodexProperty.GetInstance(nameof(CodexViewModel.ReleaseDate))!,
            CodexProperty.GetInstance(nameof(CodexViewModel.Tags))!,
            CodexProperty.GetInstance(nameof(CodexViewModel.Title))!,
            CodexProperty.GetInstance(nameof(CodexViewModel.Version))!
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
                ActivateFilter(startDateFilter, Include);
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
                    ActivateFilter(stopDateFilter, Include);
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
                    ActivateFilter(minRatFilter, Include);
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
            { "Title", nameof(CodexViewModel.SortingTitle) },
            { "Author",  nameof(CodexViewModel.AuthorsAsString) },
            { "Publisher",  nameof(CodexViewModel.Publisher) },
            { "User Rating",  nameof(CodexViewModel.Rating) },
            { "Date - Released",  nameof(CodexViewModel.ReleaseDate) },
            { "Date - Last Opened",  nameof(CodexViewModel.LastOpened)},
            { "Date - Added",  nameof(CodexViewModel.DateAdded) },
            { "Page Count",  nameof(CodexViewModel.PageCount) },
            { "Times opened", nameof(CodexViewModel.OpenedCount) }
        };

        #endregion

        #region Methods and Commands

        private void OnCodexCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            var oldCodexVms = e.OldItems?.Cast<CodexViewModel>() ?? [];
            UnSubscribeFromCodexProperties(oldCodexVms);
                
            var newCodexVms = e.NewItems?.Cast<CodexViewModel>() ?? [];
            SubscribeToCodexProperties(newCodexVms);
            
            ReFilter();
        }
        
        private void SubscribeToCodexProperties(IEnumerable<CodexViewModel> codexVms)
        {
            //cause derived lists to update when codex gets updated
            foreach (CodexViewModel c in codexVms)
            {
                c.PropertyChanged += OnCodexPropsChanged;
            }
        }
        
        private void UnSubscribeFromCodexProperties(IEnumerable<CodexViewModel> codexVms)
        {
            foreach (CodexViewModel c in codexVms)
            {
                c.PropertyChanged -= OnCodexPropsChanged;
            }
        }

        private void OnCodexPropsChanged(object? _,  PropertyChangedEventArgs e)
        {
            //Do not refilter on props that don't affect filters
            if (e.PropertyName == nameof(CodexViewModel.Cover) ||
                e.PropertyName == nameof(CodexViewModel.Thumbnail))
            {
                return;
            }

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                OnPropertyChanged(nameof(Favorites));
                OnPropertyChanged(nameof(RecentCodices));
                OnPropertyChanged(nameof(MostOpenedCodices));
                PopulateMetaDataCollections();
                ReFilter();
            });
        }
        
        private void InitSortingProperties()
        {
            //double check on typos by checking if all property names exist in codex class
            var possibleSortPropertyNames = typeof(CodexViewModel).GetProperties().Select(p => p.Name).ToList();
            if (SortOptions.Select(pair => pair.Value).Except(possibleSortPropertyNames).Any())
            {
                Logger.Warn("One of the sort property paths does not exist", new MissingMemberException());
            }
        }

        public void PopulateMetaDataCollections() => Dispatcher.UIThread.Invoke(() =>
        {
            foreach (CodexViewModel vm in _allCodexVms)
            {
                //Populate Author Collection
                AuthorList = new(AuthorList.Union(vm.Authors));

                //Populate Publisher Collection
                if (!String.IsNullOrEmpty(vm.Publisher)) PublisherList.AddIfMissing(vm.Publisher);

                //Populate FileType Collection
                if (!String.IsNullOrEmpty(vm.Sources.FileType)) FileTypeList.AddIfMissing(vm.Sources.FileType);

                //Populate Domain Collection
                if (vm.Sources.HasOnlineSource())
                {
                    string domain = Uri.IsWellFormedUriString(vm.Sources.SourceURL, UriKind.Absolute) ?
                        new Uri(vm.Sources.SourceURL).Host :
                        vm.Sources.SourceURL;
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

        public FiltersState GetFiltersState() => new FiltersState()
            {
                IncludedFilters = IncludedFilters.Select(filter => filter.GetModel()).ToList(),
                ExcludedFilters = ExcludedFilters.Select(filter => filter.GetModel()).ToList()
            };

        //------------- Adding, Removing, ect ------------//

        // Remove Filter
        private RelayCommand<FilterViewModel>? _removeFromItemsControlCommand;
        public RelayCommand<FilterViewModel> RemoveFromItemsControlCommand => _removeFromItemsControlCommand ??= new(RemoveFilter);
        public void RemoveFilter(FilterViewModel? filter)
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
        private RelayCommand<FilterViewModel>? _activateFilterCommand;
        public RelayCommand<FilterViewModel> ActivateFilterCommand => _activateFilterCommand ??= new(vm => ActivateFilter(vm, Include));

        private void ActivateFilter(FilterViewModel? filterVm, bool include = true)
        {
            if (filterVm == null) return;
            var filter = filterVm.GetModel();

            ObservableCollection<FilterViewModel> target = include ? IncludedFilters : ExcludedFilters;
            ObservableCollection<FilterViewModel> other = !include ? IncludedFilters : ExcludedFilters;

            //if Filter does not allow multiple instances, remove previous instance(s) of that Filter before adding
            if (!filter.AllowMultiple && target.Any(f => f.Type == filter.Type))
            {
                target.RemoveWhere(f => f.Type == filter.Type);
            }
            
            target.AddIfMissing(filterVm);
            other.Remove(filterVm); //filter should never occur in both include and exclude so remove from other
        }
        
        public void ActivateFilter(Filter? filter, bool include = true)
        {
            if (filter is null) return;
            FilterViewModel filterVm = ModelVmFactory.GetFilterViewModel(filter);
            ActivateFilter(filterVm, include);
        }

        private RelayCommand<string>? _searchCommand;
        public RelayCommand<string> SearchCommand => _searchCommand ??= new(SearchCommandHelper);
        private void SearchCommandHelper(string? searchTerm)
        {
            if (!String.IsNullOrEmpty(searchTerm))
            {
                Filter searchFilter = new SearchFilter(searchTerm);
                ActivateFilter(searchFilter);
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
            _includedCodices = [.. _allCodexVms];
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
        private IEnumerable<CodexViewModel> GetFilteredCodicesByType(IEnumerable<FilterViewModel> filters, FilterType filterType, bool include)
        {
            IEnumerable<FilterViewModel> relevantFilterVms = filters.Where(filter => filter.Type == filterType);
            List<Filter> relevantFilters = relevantFilterVms.Select(filterVm => filterVm.GetModel()).ToList();
            
            if (relevantFilters.Count == 0) return include ? _allCodexVms : Enumerable.Empty<CodexViewModel>();

            return filterType switch
            {
                FilterType.Tag => GetFilteredCodicesByTags(relevantFilters, include),
                _ => _allCodexVms.Where(vm => relevantFilters.Any(filter => filter.Apply(vm.GetModel())))
            };
        }

        private HashSet<CodexViewModel> GetFilteredCodicesByTags(IEnumerable<Filter> filters, bool include)
            => include ? GetIncludedCodicesByTags(filters) : GetExcludedCodicesByTags(filters);
        private HashSet<CodexViewModel> GetIncludedCodicesByTags(IEnumerable<Filter> filters)
        {
            HashSet<CodexViewModel> includedCodices = [.. _allCodexVms];

            List<Tag> includedTags = filters
                .Select(filter => ((TagViewModel)filter.FilterValue!).GetModel())
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
                    HashSet<CodexViewModel> singleGroupFilteredCodices =
                        [.. _allCodexVms.Where(codexVm => singleGroupTags.Intersect(codexVm.GetModel().Tags).Any())];

                    includedCodices = includedCodices.Intersect(singleGroupFilteredCodices).ToHashSet();
                }
            }
            return includedCodices;
        }
        private HashSet<CodexViewModel> GetExcludedCodicesByTags(IEnumerable<Filter> filters)
        {
            HashSet<CodexViewModel> excludedCodices = [];

            var excludedTags = filters.Select(filter => ((TagViewModel)filter.FilterValue!).GetModel()).ToList();

            if (excludedTags.Count > 0)
            {
                // If parent is excluded, so should all the children
                excludedTags = excludedTags.Flatten().ToList();
                excludedCodices = [.. _allCodexVms.Where(codexVm => excludedTags.Intersect(codexVm.GetModel().Tags).Any())];
            }

            return excludedCodices;
        }
        //------------------------------------//

        private void ApplySorting() => FilteredCodices?.Sort(c => c.GetPropertyValue(SortProperty), SortDirection);

        private void ApplyFilters(bool force = false)
        {
            IList<CodexViewModel> filteredCodexVms = _allCodexVms
                .Intersect(_includedCodices)
                .Except(_excludedCodices)
                .ToList();

            if (force || !FilteredCodices.SequenceEqual(filteredCodexVms))
            {
                FilteredCodices.Clear();
                FilteredCodices.AddRange(filteredCodexVms);
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

        #endregion

        #region Drag Drop Handlers
        //Drop on Treeview Behaviour
        void OnDragOver(object sender, DragEventArgs e)
        {
            //Move From Treeview
            if (e.Data.GetValue<TreeNode<Tag>>() is { Item.IsGroup: false })
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
            if (e.Data.GetValue<TreeNode<TagViewModel>>() is { Item.IsGroup: false } node)
            {
                ActivateFilter(new TagFilter(node.Item), toIncluded);
            }
            //Move Filter to included/excluded
            else if (e.Data.GetValue<Filter>() is Filter draggedFilter)
            {
                ActivateFilter(draggedFilter, toIncluded);
            }
        }
        #endregion
    }
}
