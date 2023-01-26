using COMPASS.Models;
using COMPASS.Properties;
using COMPASS.Tools;
using COMPASS.ViewModels.Commands;
using FuzzySharp;
using GongSolutions.Wpf.DragDrop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace COMPASS.ViewModels
{
    public class CollectionViewModel : ViewModelBase, IDropTarget
    {

        //Constuctor
        public CollectionViewModel(CodexCollection CurrentCollection)
        {
            _cc = CurrentCollection;

            ActiveFiles = new(_cc.AllCodices);

            //load sorting from settings
            InitSortingProperties();
            ApplySorting();

            ExcludedCodicesByTag = new();
            ExcludedCodicesByExcludedTags = new();
            ExcludedCodicesByFilter = new();

            SearchTerm = "";
            SourceFilters = new()
            {
                new(Enums.FilterType.OfflineSource)
                {
                    Label = "Available Offline",
                    BackgroundColor = Colors.DarkSeaGreen
                },

                new(Enums.FilterType.OnlineSource)
                {
                    Label = "Available Online",
                    BackgroundColor = Colors.DarkSeaGreen
                },

                new(Enums.FilterType.PhysicalSource)
                {
                    Label = "Physically Owned",
                    BackgroundColor = Colors.DarkSeaGreen
                },
            };

            ActiveTags = new();
            ActiveTags.CollectionChanged += (e, v) => UpdateTagFilteredFiles();
            DeActiveTags = new();
            DeActiveTags.CollectionChanged += (e, v) => UpdateTagFilteredFiles();
            ActiveFilters = new();
            ActiveFilters.CollectionChanged += (e, v) => UpdateFieldFilteredFiles();
            DeActiveFilters = new();
            DeActiveFilters.CollectionChanged += (e, v) => UpdateFieldFilteredFiles();

            _cc.AllCodices.CollectionChanged += (e, v) => SubscribeToCodexProperties();
            SubscribeToCodexProperties();
        }

        #region Properties
        readonly CodexCollection _cc;
        private readonly int _itemsShown = 15;
        public int ItemsShown => Math.Min(_itemsShown, ActiveFiles.Count);

        //CollectionDirectories
        public ObservableCollection<Tag> ActiveTags { get; set; }
        public ObservableCollection<Tag> DeActiveTags { get; set; }
        public ObservableCollection<Filter> ActiveFilters { get; set; }
        public ObservableCollection<Filter> DeActiveFilters { get; set; }

        public HashSet<Codex> ExcludedCodicesByTag { get; set; }
        public HashSet<Codex> ExcludedCodicesByExcludedTags { get; set; }
        public HashSet<Codex> ExcludedCodicesByFilter { get; set; }

        private ObservableCollection<Codex> _activeFiles;
        public ObservableCollection<Codex> ActiveFiles
        {
            get { return _activeFiles; }
            set { SetProperty(ref _activeFiles, value); }
        }

        public ObservableCollection<Codex> Favorites => new(ActiveFiles.Where(c => c.Favorite));
        public List<Codex> RecentCodices => ActiveFiles.OrderByDescending(c => c.LastOpened).ToList().GetRange(0, ItemsShown);
        public List<Codex> MostOpenedCodices => ActiveFiles.OrderByDescending(c => c.OpenedCount).ToList().GetRange(0, ItemsShown);
        public List<Codex> RecentlyAddedCodices => ActiveFiles.OrderByDescending(c => c.DateAdded).ToList().GetRange(0, ItemsShown);

        private string _searchTerm;
        public string SearchTerm
        {
            get { return _searchTerm; }
            set { SetProperty(ref _searchTerm, value); }
        }

        private List<Filter> _sourceFilters;
        public List<Filter> SourceFilters
        {
            get { return _sourceFilters; }
            init { SetProperty(ref (_sourceFilters), value); }
        }

        public ListSortDirection SortDirection
        {
            get { return (ListSortDirection)Settings.Default[nameof(SortDirection)]; }
            set
            {
                Settings.Default[nameof(SortDirection)] = (int)value;
                Settings.Default.Save();
                ApplySorting();
                RaisePropertyChanged(nameof(SortDirection));
            }
        }

        public string SortProperty
        {
            get { return (string)Settings.Default[nameof(SortProperty)]; }
            set
            {
                Settings.Default[nameof(SortProperty)] = value;
                Settings.Default.Save();
                ApplySorting();
                RaisePropertyChanged(nameof(SortProperty));
            }
        }

        private readonly Dictionary<string, string> _sortOptions = new()
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
        public Dictionary<string, string> SortOptions => _sortOptions;
        #endregion

        #region Functions
        public void SubscribeToCodexProperties()
        {
            //cause derived lists to update when codex gets updated
            foreach (Codex c in _cc.AllCodices)
            {
                c.PropertyChanged += (e, v) => RaisePropertyChanged(nameof(Favorites));
                c.PropertyChanged += (e, v) => RaisePropertyChanged(nameof(RecentCodices));
                c.PropertyChanged += (e, v) => RaisePropertyChanged(nameof(MostOpenedCodices));
            }
        }

        private ActionCommand _clearFiltersCommand;
        public ActionCommand ClearFiltersCommand => _clearFiltersCommand ??= new(ClearFilters);
        public void ClearFilters()
        {
            SearchTerm = "";
            ActiveTags.Clear();
            DeActiveTags.Clear();
            ActiveFilters.Clear();
            DeActiveFilters.Clear();
            ActiveFiles = new(_cc.AllCodices);
        }

        //-------------For Tags---------------//
        public void UpdateTagFilteredFiles()
        {
            ExcludedCodicesByTag.Clear();
            HashSet<Tag> ActiveGroups = new();

            //Find all the active groups to filter in
            foreach (Tag tag in ActiveTags)
            {
                ActiveGroups.Add(tag.GetGroup());
            }

            //List of Files filtered out in that group
            HashSet<Codex> SingleGroupFilteredFiles;
            //Go over every group and filter out files
            foreach (Tag Group in ActiveGroups)
            {
                //Make list with all active tags in that group, including childeren
                List<Tag> SingleGroupTags = Utils.FlattenTree(ActiveTags.Where(tag => tag.GetGroup() == Group)).ToList();
                //add parents of those tags, must come AFTER chileren, otherwise childeren of parents are included which is wrong
                for (int i = 0; i < SingleGroupTags.Count; i++)
                {
                    Tag P = SingleGroupTags[i].Parent;
                    if (P != null && !P.IsGroup && !SingleGroupTags.Contains(P)) SingleGroupTags.Add(P);
                }
                SingleGroupFilteredFiles = new(_cc.AllCodices.Where(f => !SingleGroupTags.Intersect(f.Tags).Any()));

                ExcludedCodicesByTag = ExcludedCodicesByTag.Union(SingleGroupFilteredFiles).ToHashSet();
            }

            //Filter out Codices with excluded tags
            //get childeren too, if parent is excluded, so should all the childeren
            List<Tag> AllDeActiveTags = Utils.FlattenTree(DeActiveTags).ToList();
            ExcludedCodicesByExcludedTags = new(_cc.AllCodices.Where(f => AllDeActiveTags.Intersect(f.Tags).Any()));

            UpdateActiveFiles();
        }

        private RelayCommand<Tag> _addTagFilterCommand;
        public RelayCommand<Tag> AddTagFilterCommand => _addTagFilterCommand ??= new(AddTagFilterHelper);
        private void AddTagFilterHelper(Tag tag) => AddTagFilter(tag); //needed because relaycommand only takes functions with one arg
        public void AddTagFilter(Tag t, bool include = true)
        {
            //Move to active if include and not yet in active
            if (include && !ActiveTags.Contains(t))
            {
                ActiveTags.Add(t);
                DeActiveTags.Remove(t);
            }

            //Move to deactive if include and not yet in deactive
            if (!include && !DeActiveTags.Contains(t))
            {
                DeActiveTags.Add(t);
                ActiveTags.Remove(t);
            }
        }

        private RelayCommand<ITag> _removeFilterCommand;
        public RelayCommand<ITag> RemoveFilterCommand => _removeFilterCommand ??= new(RemoveFilter);
        public void RemoveFilter(ITag tag)
        {
            switch (tag)
            {
                case Tag t:
                    RemoveTagFilter(t);
                    break;
                case Filter f:
                    RemoveFieldFilter(f);
                    break;
            }
        }
        public void RemoveTagFilter(Tag t)
        {
            ActiveTags.Remove(t);
            DeActiveTags.Remove(t);
        }

        //-------------For Filters------------//
        public void UpdateFieldFilteredFiles()
        {
            ExcludedCodicesByFilter.Clear();

            //enumerate over all filter types
            foreach (Enums.FilterType FT in Enum.GetValues(typeof(Enums.FilterType)))
            {
                ExcludedCodicesByFilter = ExcludedCodicesByFilter.Union(GetFieldFilteredCodices(FT, ActiveFilters)).ToHashSet();
                ExcludedCodicesByFilter = ExcludedCodicesByFilter.Union(GetFieldFilteredCodices(FT, DeActiveFilters, true)).ToHashSet();
            }
            UpdateActiveFiles();
        }

        public List<Codex> GetFieldFilteredCodices(Enums.FilterType filtertype, IEnumerable<Filter> Filters, bool invert = false)
        {
            List<object> FilterValues = new(
                    Filters
                    .Where(filter => filter.Type == filtertype)
                    .Select(t => t.FilterValue)
                    );

            if (FilterValues.Count == 0) return new();

            IEnumerable<Codex> ExcludedCodices = new List<Codex>(); // generic IEnumerable doesn'tag have constructor so list instead
            switch (filtertype)
            {
                case Enums.FilterType.Search:
                    ExcludedCodices = GetSearchFilteredCodices((string)FilterValues.FirstOrDefault());
                    break;
                case Enums.FilterType.Author:
                    ExcludedCodices = _cc.AllCodices.Where(f => !FilterValues.Intersect(f.Authors).Any());
                    break;
                case Enums.FilterType.Publisher:
                    ExcludedCodices = _cc.AllCodices.Where(f => !FilterValues.Contains(f.Publisher));
                    break;
                case Enums.FilterType.StartReleaseDate:
                    ExcludedCodices = _cc.AllCodices.Where(f => f.ReleaseDate < (DateTime?)FilterValues.FirstOrDefault());
                    break;
                case Enums.FilterType.StopReleaseDate:
                    ExcludedCodices = _cc.AllCodices.Where(f => f.ReleaseDate > (DateTime?)FilterValues.FirstOrDefault());
                    break;
                case Enums.FilterType.MinimumRating:
                    ExcludedCodices = _cc.AllCodices.Where(f => f.Rating < (int?)FilterValues.FirstOrDefault());
                    break;
                case Enums.FilterType.OfflineSource:
                    ExcludedCodices = _cc.AllCodices.Where(f => !f.HasOfflineSource());
                    break;
                case Enums.FilterType.OnlineSource:
                    ExcludedCodices = _cc.AllCodices.Where(f => !f.HasOnlineSource());
                    break;
                case Enums.FilterType.PhysicalSource:
                    ExcludedCodices = _cc.AllCodices.Where(f => !f.Physically_Owned);
                    break;
            }
            return invert ? _cc.AllCodices.Except(ExcludedCodices).ToList() : ExcludedCodices.ToList();
        }

        public HashSet<Codex> GetSearchFilteredCodices(string searchterm, bool returnExcludedCodices = true)
        {
            HashSet<Codex> ExcludedCodicesBySearch = new();
            HashSet<Codex> IncludedCodicesBySearch = new();
            if (!string.IsNullOrEmpty(searchterm))
            {
                //include acronyms
                IncludedCodicesBySearch.UnionWith(_cc.AllCodices
                    .Where(f => Fuzz.TokenInitialismRatio(f.Title.ToLowerInvariant(), SearchTerm) > 80));
                //include string fragments
                IncludedCodicesBySearch.UnionWith(_cc.AllCodices
                    .Where(f => f.Title.Contains(SearchTerm, StringComparison.InvariantCultureIgnoreCase)));
                //include spelling errors
                //include acronyms
                IncludedCodicesBySearch.UnionWith(_cc.AllCodices
                    .Where(f => Fuzz.PartialRatio(f.Title.ToLowerInvariant(), SearchTerm) > 80));

                ExcludedCodicesBySearch = new(_cc.AllCodices.Except(IncludedCodicesBySearch));
            }
            return returnExcludedCodices ? ExcludedCodicesBySearch : IncludedCodicesBySearch;
        }

        public void AddFieldFilter(Filter filter, bool include = true)
        {
            ObservableCollection<Filter> Target = include ? ActiveFilters : DeActiveFilters;
            ObservableCollection<Filter> Other = !include ? ActiveFilters : DeActiveFilters;
            //if Filter is unique, remove previous instance of that Filter before adding
            if (filter.Unique)
            {
                Target.Remove(
                    Target.SingleOrDefault(f => f.Type == filter.Type));
            }
            //only add if not yet in activetags
            if (!Target.Contains(filter))
            {
                Target.Add(filter);
                Other.Remove(filter);
            }
        }

        public void RemoveFieldFilter(Filter filter)
        {
            ActiveFilters.Remove(filter);
            DeActiveFilters.Remove(filter);
        }
        //------------------------------------//

        private void InitSortingProperties()
        {
            //double check on typos by checking if all property names exist in codex class
            var PossibleSortProptertyNames = typeof(Codex).GetProperties().Select(p => p.Name).ToList();
            if (_sortOptions.Select(pair => pair.Value).Except(PossibleSortProptertyNames).Any())
            {
                MessageBox.Show("One of the sort property paths does not exist");
                Logger.log.Error("One of the sort property paths does not exist");
            }
        }
        public void ApplySorting()
        {
            var sortDescr = CollectionViewSource.GetDefaultView(ActiveFiles).SortDescriptions;
            sortDescr.Clear();
            sortDescr.Add(new SortDescription(SortProperty, SortDirection));
        }

        public void ReFilter()
        {
            UpdateTagFilteredFiles();
            UpdateFieldFilteredFiles();
            UpdateActiveFiles();
        }

        public void UpdateActiveFiles()
        {
            //compile list of "active" files, which are files that match all the different filters
            ActiveFiles = new(_cc.AllCodices
                .Except(ExcludedCodicesByTag)
                .Except(ExcludedCodicesByExcludedTags)
                .Except(ExcludedCodicesByFilter)
                .ToList());

            //Also apply filtering to these lists
            RaisePropertyChanged(nameof(Favorites));
            RaisePropertyChanged(nameof(RecentCodices));
            RaisePropertyChanged(nameof(MostOpenedCodices));
            RaisePropertyChanged(nameof(RecentlyAddedCodices));
        }

        public void RemoveCodex(Codex c)
        {
            ExcludedCodicesByTag.Remove(c);
            ExcludedCodicesByExcludedTags.Remove(c);
            ExcludedCodicesByFilter.Remove(c);
            ActiveFiles.Remove(c);
        }

        #endregion

        #region Drag Drop Handlers
        //Drop on Treeview Behaviour
        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            //Tree to Filter Box
            switch (dropInfo.Data)
            {
                //Move From Treeview
                case TreeViewNode DraggedTVN:
                    if (!DraggedTVN.Tag.IsGroup)
                    {
                        dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                        dropInfo.Effects = DragDropEffects.Copy;
                    }
                    break;
                //Move Filter included/excluded
                case Filter:
                //Do filter specific stuff here if needed
                //Move Tag between included/excluded
                case Tag:
                    dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                    dropInfo.Effects = DragDropEffects.Move;
                    break;
            }
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            //Included filter Listbox has extra empty collection to tell the difference
            bool ToIncluded = ((CompositeCollection)(dropInfo.TargetCollection)).Count > 2;

            switch (dropInfo.Data)
            {
                //Tree to Filter Box
                case TreeViewNode DraggedTVN:
                    AddTagFilter(DraggedTVN.Tag, ToIncluded);
                    break;
                //Between include and exlude
                case Filter DraggedFilter:
                    AddFieldFilter(DraggedFilter, ToIncluded);
                    break;
                case Tag DraggedTag:
                    AddTagFilter(DraggedTag, ToIncluded);
                    break;
            }
        }
        #endregion
    }
}
