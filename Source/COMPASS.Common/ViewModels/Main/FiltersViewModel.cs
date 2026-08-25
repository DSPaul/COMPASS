using Avalonia.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Adorners;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Models.DragDrop;
using COMPASS.Common.Models.Filters;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Models;
using COMPASS.Infra.Avalonia.DragDrop;
using COMPASS.Infra.Avalonia.ExtensionMethods;
using COMPASS.Infra.Tools;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace COMPASS.Common.ViewModels.Main
{
    public class FiltersViewModel : ViewModelBase
    {
        public FiltersViewModel(CodexCollectionVM collectionVM, FiltersState? filtersState = null)
        {
            _allCodexVms = collectionVM.AllCodexVms;
            _filterService = ServiceResolver.Resolve<IFilterService>();

            //We want a single event at the end of the constructor 
            _codicesUpdatedNotifier = new (e => CodicesUpdated?.Invoke(this, e));
            using var updateScope = DelayUpdateEvents();
                
            // Load sorting from settings
            InitSortingProperties();

            if (filtersState != null)
            {
                IncludedFilters = new(filtersState.IncludedFilters.Select(ModelVmFactory.GetFilterViewModel));
                ExcludedFilters = new(filtersState.ExcludedFilters.Select(ModelVmFactory.GetFilterViewModel));
            }

            IncludedFilters.CollectionChanged += HandleRefilter;
            ExcludedFilters.CollectionChanged += HandleRefilter;
            collectionVM.AllCodexVms.CollectionChanged += (_, _) =>
            {
                PopulateMetaDataCollections();
                TriggerFilter();
            };
            collectionVM.CodexPropertyChanged += OnCodexPropertyChanged;

            PopulateMetaDataCollections();

            TriggerFilter();

            IncludedDropManager = CreateFilterDropManager(include: true);
            ExcludedDropManager = CreateFilterDropManager(include: false);
        }

        public event EventHandler? CodicesUpdated;
        private readonly EventDeferralScope _codicesUpdatedNotifier;
        private IFilterService _filterService;

        #region Fields

        private readonly IPreferencesService _preferencesService = ServiceResolver.Resolve<IPreferencesService>();
        
        private readonly RangeObservableCollection<CodexViewModel> _allCodexVms;

        
        #endregion

        #region Properties

        public bool Include
        {
            get;
            set => SetProperty(ref field, value);
        } = true;

        public RangeObservableCollection<FilterViewModel> IncludedFilters { get; } = [];
        public RangeObservableCollection<FilterViewModel> ExcludedFilters { get; } = [];
        public bool HasActiveFilters => IncludedFilters.Any() || ExcludedFilters.Any();

        public RangeObservableCollection<CodexViewModel> FilteredCodices { get; } = [];

        public string SearchTerm
        {
            get;
            set => SetProperty(ref field, value);
        } = "";

        private List<Filter> _booleanFilters =
        [
            new OfflineSourceFilter(),
            new OnlineSourceFilter(),
            new PhysicalSourceFilter(),
            new FavoriteFilter()
        ];

        public List<FilterViewModel> BooleanFilters => field ??= _booleanFilters.Select(ModelVmFactory.GetFilterViewModel).ToList();

        public string SelectedAuthor
        {
            set
            {
                if (String.IsNullOrEmpty(value)) return;
                Filter authorFilter = new AuthorFilter(value);
                ActivateFilter(authorFilter, Include);
            }
        }

        public ObservableCollection<string> AuthorList
        {
            get;
            set => SetProperty(ref field, value);
        } = [];

        public string SelectedPublisher
        {
            set
            {
                if (String.IsNullOrEmpty(value)) return;
                Filter publisherFilter = new PublisherFilter(value);
                ActivateFilter(publisherFilter, Include);
            }
        }

        public ObservableCollection<string> PublisherList
        {
            get;
            set => SetProperty(ref field, value);
        } = [];

        public string SelectedFileType
        {
            set
            {
                if (String.IsNullOrEmpty(value)) return;
                Filter fileExtensionFilter = new FileExtensionFilter(value);
                ActivateFilter(fileExtensionFilter, Include);
            }
        }

        public ObservableCollection<string> FileTypeList
        {
            get;
            set => SetProperty(ref field, value);
        } = [];

        public string SelectedDomain
        {
            set
            {
                if (String.IsNullOrEmpty(value)) return;
                Filter domainFilter = new DomainFilter(value);
                ActivateFilter(domainFilter, Include);
            }
        }

        public ObservableCollection<string> DomainList
        {
            get;
            set => SetProperty(ref field, value);
        } = [];

        public CodexProperty SelectedNotEmptyProperty
        {
            set
            {
                if (value is not null)
                {
                    Filter notEmptyFilter = new NotEmptyFilter(value);
                    ActivateFilter(notEmptyFilter, Include);
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

        public DateTime? StartReleaseDate
        {
            get;
            set
            {
                SetProperty(ref field, value);
                if (value is null) return;
                Filter startDateFilter = new StartReleaseDateFilter(value.Value);
                ActivateFilter(startDateFilter, Include);
            }
        }

        public DateTime? StopReleaseDate
        {
            get;
            set
            {
                SetProperty(ref field, value);
                if (value != null)
                {
                    Filter stopDateFilter = new StopReleaseDateFilter(value.Value);
                    ActivateFilter(stopDateFilter, Include);
                }
            }
        }

        //Selected minimum rating
        public int MinRating
        {
            get;
            set
            {
                SetProperty(ref field, value);
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
                    SortAndNotify();
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
                    SortAndNotify();
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

        #region Event handlers

        private void OnCodexPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CodexViewModel.Authors) ||
                e.PropertyName == nameof(CodexViewModel.Publisher) ||
                e.PropertyName == nameof(CodexViewModel.Sources))
            {
                PopulateMetaDataCollections();
            }

            bool influencesSort = e.PropertyName == SortProperty;
            bool influencesFilter = IncludedFilters.Concat(ExcludedFilters)
                                                   .SelectMany(filerVM => filerVM.GetModel().RelatedProperties)
                                                   .Contains(e.PropertyName);

            if (influencesFilter)
            {
                TriggerFilter();
            }
            else if (influencesSort)
            {
                SortAndNotify();
            }
        }

        private void HandleRefilter(object? sender, EventArgs e)
        {
            TriggerFilter();
        }

        #endregion

        #region Methods and Commands
        
        private void InitSortingProperties()
        {
            //double check on typos by checking if all property names exist in codex class
            var possibleSortPropertyNames = typeof(CodexViewModel).GetProperties().Select(p => p.Name).ToList();
            if (SortOptions.Select(pair => pair.Value).Except(possibleSortPropertyNames).Any())
            {
                Logger.Warn("One of the sort property paths does not exist", new MissingMemberException());
            }
        }

        public void UpdateSortProperty(string sortProperty)
        {
            if (SortProperty == sortProperty)
            {
                SortAscending = !SortAscending;
            }
            else
            {
                SortProperty = sortProperty;
                SortAscending = true;
            }
        }

        public void PopulateMetaDataCollections()
        {
            //snapshot on the UI thread because _allCodexVms may be mutated there while we enumerate
            List<CodexViewModel> codexVmsSnapshot = [.. _allCodexVms];

            //put this on a background thread
            Task.Run(() =>
            {
                HashSet<string> authors = [];
                HashSet<string> publishers = [];
                HashSet<string> fileTypes = [];
                HashSet<string> domains = [];
                Dictionary<string, string> domainCache = [];

                foreach (CodexViewModel vm in codexVmsSnapshot)
                {
                    //Populate Author Collection
                    authors.UnionWith(vm.Authors);

                    //Populate Publisher Collection
                    if (!String.IsNullOrEmpty(vm.Publisher)) publishers.Add(vm.Publisher);

                    //Populate FileType Collection
                    if (!String.IsNullOrEmpty(vm.Sources.FileType)) fileTypes.Add(vm.Sources.FileType);

                    //Populate Domain Collection
                    if (vm.Sources.HasOnlineSource())
                    {
                        string sourceURL = vm.Sources.SourceURL;
                        if (!domainCache.TryGetValue(sourceURL, out string? domain))
                        {
                            domain = Uri.TryCreate(sourceURL, UriKind.Absolute, out Uri? parsedUri) ?
                                parsedUri.Host :
                                sourceURL;
                            domainCache[sourceURL] = domain;
                        }
                        if (!string.IsNullOrEmpty(domain)) domains.Add(domain);
                    }
                }
                authors.Remove(""); //remove "" author because String.IsNullOrEmpty cannot be called during Union

                //Sort & apply them
                Dispatcher.UIThread.Post(() =>
                {
                    AuthorList = new(authors.Order());
                    PublisherList = new(publishers.Order());
                    FileTypeList = new(fileTypes.Order());
                    DomainList = new(domains.Order());
                });
            });
        }

        public FiltersState GetFiltersState() => new FiltersState()
            {
                IncludedFilters = IncludedFilters.Select(filter => filter.GetModel()).ToList(),
                ExcludedFilters = ExcludedFilters.Select(filter => filter.GetModel()).ToList()
            };

        //------------- Adding, Removing, ect ------------//

        // Remove Filter
        public RelayCommand<FilterViewModel> RemoveFilterCommand => field ??= new(RemoveFilter);
        public void RemoveFilter(FilterViewModel? filter)
        {
            if (filter is null) return;
            IncludedFilters.Remove(filter);
            ExcludedFilters.Remove(filter);
        }
        public void RemoveFilterType(FilterType filterType)
        {
            IncludedFilters.RemoveAll(filter => filter.Type == filterType);
            ExcludedFilters.RemoveAll(filter => filter.Type == filterType);
        }

        // Add Filter
        public RelayCommand<FilterViewModel> ActivateFilterCommand => field ??= new(vm => ActivateFilter(vm, Include));

        private void ActivateFilter(FilterViewModel? filterVm, bool include = true)
        {
            if (filterVm == null) return;
            var filter = filterVm.GetModel();

            var target = include ? IncludedFilters : ExcludedFilters;
            var other = !include ? IncludedFilters : ExcludedFilters;

            using var updateScope = DelayUpdateEvents();
            
            //if Filter does not allow multiple instances, remove previous instance(s) of that Filter before adding
            if (!filter.AllowMultiple && target.Any(f => f.Type == filter.Type))
            {
                target.RemoveAll(f => f.Type == filter.Type);
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

        public RelayCommand<string> SearchCommand => field ??= new(SearchCommandHelper);
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
        public RelayCommand ClearFiltersCommand => field ??= new(ClearFilters);
        public void ClearFilters()
        {
            StartReleaseDate = null;
            StopReleaseDate = null;
            MinRating = 0;
            SearchTerm = "";

            IncludedFilters.Clear();
            ExcludedFilters.Clear();
        }

        private void SortAndNotify()
        {
            _filterService.SortCodices(FilteredCodices, SortProperty, SortDirection);
            _codicesUpdatedNotifier.Notify();
        }

        public void TriggerFilter(bool force = false)
        {
            try
            {
                IList<CodexViewModel> filteredCodexVms = _filterService.FilterCodices(_allCodexVms, GetFiltersState());

                if (force || !FilteredCodices.SequenceEqual(filteredCodexVms))
                {
                    Dispatcher.UIThread.PostIfNeeded(() =>
                    {
                        FilteredCodices.ReplaceRange(filteredCodexVms);
                        SortAndNotify();  
                    });
                }

            }
            catch (Exception ex)
            {
                Logger.Warn("Something when wrong during filtering", ex);
            }
        }
        
        public EventDeferralScope.DeferralScope DelayUpdateEvents() => _codicesUpdatedNotifier.BeginDeferral();
        
        #endregion
        
        #region Drop Handlers

        public DropManager IncludedDropManager { get; }
        public DropManager ExcludedDropManager { get; }

        private DropManager CreateFilterDropManager(bool include) => new DropManager()
            .AddHandler(new DropHandler<Filter>(DataTransferFormats.FilterFormat, DragDropEffects.Move)
            {
                OnDroppedSingle = filter => ActivateFilter(filter, include),
                CanDrop = filter => include
                    ? !IncludedFilters.Any(vm => vm.GetModel() == filter)
                    : !ExcludedFilters.Any(vm => vm.GetModel() == filter),
                AdornerFactory = filters => new DropFilterAdorner(filters[0]) { Format = "Move {0} here" },
            })
            .AddHandler(new DropHandler<Tag>(DataTransferFormats.TagFormat, DragDropEffects.Link)
            {
                OnDroppedSingle = tag =>
                {
                    var tagVm = TabsViewModel.GetInstance()?.ActiveTab?.CollectionVM.GetTagVm(tag);
                    if (tagVm is not null)
                        ActivateFilter(new TagFilter(tagVm), include);
                },
                CanDrop = tag => !tag.IsGroup,
                AdornerFactory = tags => new DropTagAdorner(tags[0]) { Format = "Filter on {0}" },
            });
        #endregion
    }
}
