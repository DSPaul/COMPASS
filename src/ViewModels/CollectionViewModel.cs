using COMPASS.Models;
using COMPASS.Properties;
using COMPASS.Tools;
using COMPASS.Commands;
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

            //load sorting from settings
            InitSortingProperties();

            ExcludedCodicesByIncludedTags = new();
            ExcludedCodicesByExcludedTags = new();
            ExcludedCodicesByFilters = new();

            SearchTerm = "";
            SourceFilters = new()
            {
                new(Filter.FilterType.OfflineSource)
                {
                    Label = "Available Offline",
                    BackgroundColor = Colors.DarkSeaGreen
                },

                new(Filter.FilterType.OnlineSource)
                {
                    Label = "Available Online",
                    BackgroundColor = Colors.DarkSeaGreen
                },

                new(Filter.FilterType.PhysicalSource)
                {
                    Label = "Physically Owned",
                    BackgroundColor = Colors.DarkSeaGreen
                },
            };

            IncludedTags = new();
            IncludedTags.CollectionChanged += (e, v) => SetExcludedCodicesByTags();
            ExcludedTags = new();
            ExcludedTags.CollectionChanged += (e, v) => SetExcludedCodicesByTags();
            IncludedFilters = new();
            IncludedFilters.CollectionChanged += (e, v) => SetExcludedCodicesByFilters();
            ExcludedFilters = new();
            ExcludedFilters.CollectionChanged += (e, v) => SetExcludedCodicesByFilters();

            _cc.AllCodices.CollectionChanged += (e, v) => SubscribeToCodexProperties();
            SubscribeToCodexProperties();

            ApplyFilters();
        }

        #region Properties
        readonly CodexCollection _cc;
        private readonly int _itemsShown = 15;
        public int ItemsShown => Math.Min(_itemsShown, FilteredCodices.Count);

        public ObservableCollection<Tag> IncludedTags { get; set; }
        public ObservableCollection<Tag> ExcludedTags { get; set; }
        public ObservableCollection<Filter> IncludedFilters { get; set; }
        public ObservableCollection<Filter> ExcludedFilters { get; set; }

        private HashSet<Codex> ExcludedCodicesByIncludedTags { get; set; }
        private HashSet<Codex> ExcludedCodicesByExcludedTags { get; set; }
        private HashSet<Codex> ExcludedCodicesByFilters { get; set; }

        private ObservableCollection<Codex> _filteredCodices;
        public ObservableCollection<Codex> FilteredCodices
        {
            get => _filteredCodices;
            set => SetProperty(ref _filteredCodices, value);
        }

        public ObservableCollection<Codex> Favorites => new(FilteredCodices.Where(c => c.Favorite));
        public List<Codex> RecentCodices => FilteredCodices.OrderByDescending(c => c.LastOpened).ToList().GetRange(0, ItemsShown);
        public List<Codex> MostOpenedCodices => FilteredCodices.OrderByDescending(c => c.OpenedCount).ToList().GetRange(0, ItemsShown);
        public List<Codex> RecentlyAddedCodices => FilteredCodices.OrderByDescending(c => c.DateAdded).ToList().GetRange(0, ItemsShown);

        private string _searchTerm;
        public string SearchTerm
        {
            get => _searchTerm;
            set => SetProperty(ref _searchTerm, value);
        }

        private List<Filter> _sourceFilters;
        public List<Filter> SourceFilters
        {
            get => _sourceFilters;
            init => SetProperty(ref _sourceFilters, value);
        }

        public ListSortDirection SortDirection
        {
            get => (ListSortDirection)Settings.Default[nameof(SortDirection)];
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
            get => (string)Settings.Default[nameof(SortProperty)];
            set
            {
                Settings.Default[nameof(SortProperty)] = value;
                Settings.Default.Save();
                ApplySorting();
                RaisePropertyChanged(nameof(SortProperty));
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
            foreach (Codex c in _cc.AllCodices)
            {
                c.PropertyChanged += (e, v) => RaisePropertyChanged(nameof(Favorites));
                c.PropertyChanged += (e, v) => RaisePropertyChanged(nameof(RecentCodices));
                c.PropertyChanged += (e, v) => RaisePropertyChanged(nameof(MostOpenedCodices));
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

        private ActionCommand _clearFiltersCommand;
        public ActionCommand ClearFiltersCommand => _clearFiltersCommand ??= new(ClearFilters);
        public void ClearFilters()
        {
            SearchTerm = "";
            IncludedTags.Clear();
            ExcludedTags.Clear();
            IncludedFilters.Clear();
            ExcludedFilters.Clear();
        }

        //-------------For Tags---------------//
        private void SetExcludedCodicesByTags()
        {
            ExcludedCodicesByIncludedTags.Clear();
            HashSet<Tag> IncludedGroups = new();

            //Find all the groups to filter in
            foreach (Tag tag in IncludedTags)
            {
                IncludedGroups.Add(tag.GetGroup());
            }

            //List of codices filtered out in that group
            HashSet<Codex> SingleGroupFilteredCodices;
            //Go over every group and filter out codices
            foreach (Tag Group in IncludedGroups)
            {
                //Make list with all included tags in that group, including childeren
                List<Tag> SingleGroupTags = Utils.FlattenTree(IncludedTags.Where(tag => tag.GetGroup() == Group)).ToList();
                //add parents of those tags, must come AFTER chileren, otherwise childeren of parents are included which is wrong
                for (int i = 0; i < SingleGroupTags.Count; i++)
                {
                    Tag P = SingleGroupTags[i].Parent;
                    if (P != null && !P.IsGroup && !SingleGroupTags.Contains(P)) SingleGroupTags.Add(P);
                }
                SingleGroupFilteredCodices = new(_cc.AllCodices.Where(f => !SingleGroupTags.Intersect(f.Tags).Any()));

                ExcludedCodicesByIncludedTags = ExcludedCodicesByIncludedTags.Union(SingleGroupFilteredCodices).ToHashSet();
            }

            //Filter out Codices with excluded tags
            //get childeren too, if parent is excluded, so should all the childeren
            List<Tag> AllExcludedTags = Utils.FlattenTree(ExcludedTags).ToList();
            ExcludedCodicesByExcludedTags = new(_cc.AllCodices.Where(f => AllExcludedTags.Intersect(f.Tags).Any()));

            ApplyFilters();
        }

        public void AddTagFilter(Tag t, bool include = true)
        {
            if (include && !IncludedTags.Contains(t))
            {
                IncludedTags.Add(t);
                ExcludedTags.Remove(t);
            }

            if (!include && !ExcludedTags.Contains(t))
            {
                ExcludedTags.Add(t);
                IncludedTags.Remove(t);
            }
        }

        public void RemoveTagFilter(Tag t)
        {
            IncludedTags.Remove(t);
            ExcludedTags.Remove(t);
        }

        //-------------For Filters------------//
        private void SetExcludedCodicesByFilters()
        {
            ExcludedCodicesByFilters.Clear();

            //enumerate over all filter types
            foreach (Filter.FilterType FT in Enum.GetValues(typeof(Filter.FilterType)))
            {
                ExcludedCodicesByFilters = ExcludedCodicesByFilters.Union(GetFilteredCodicesByProperty(FT, IncludedFilters)).ToHashSet();
                ExcludedCodicesByFilters = ExcludedCodicesByFilters.Union(GetFilteredCodicesByProperty(FT, ExcludedFilters, false)).ToHashSet();
            }
            ApplyFilters();
        }

        //Return List of Codices that do Do/Don't match filter on a property (author, release date, ect.)
        private List<Codex> GetFilteredCodicesByProperty(Filter.FilterType filtertype, IEnumerable<Filter> Filters, bool returnExcludedCodices = true)
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
                case Filter.FilterType.Search:
                    ExcludedCodices = GetFilteredCodicesBySearch((string)FilterValues.FirstOrDefault());
                    break;
                case Filter.FilterType.Author:
                    ExcludedCodices = _cc.AllCodices.Where(c => !FilterValues.Intersect(c.Authors).Any());
                    break;
                case Filter.FilterType.Publisher:
                    ExcludedCodices = _cc.AllCodices.Where(c => !FilterValues.Contains(c.Publisher));
                    break;
                case Filter.FilterType.StartReleaseDate:
                    ExcludedCodices = _cc.AllCodices.Where(c => c.ReleaseDate < (DateTime?)FilterValues.FirstOrDefault());
                    break;
                case Filter.FilterType.StopReleaseDate:
                    ExcludedCodices = _cc.AllCodices.Where(c => c.ReleaseDate > (DateTime?)FilterValues.FirstOrDefault());
                    break;
                case Filter.FilterType.MinimumRating:
                    ExcludedCodices = _cc.AllCodices.Where(c => c.Rating < (int?)FilterValues.FirstOrDefault());
                    break;
                case Filter.FilterType.OfflineSource:
                    ExcludedCodices = _cc.AllCodices.Where(c => !c.HasOfflineSource());
                    break;
                case Filter.FilterType.OnlineSource:
                    ExcludedCodices = _cc.AllCodices.Where(c => !c.HasOnlineSource());
                    break;
                case Filter.FilterType.PhysicalSource:
                    ExcludedCodices = _cc.AllCodices.Where(c => !c.Physically_Owned);
                    break;
                case Filter.FilterType.FileExtension:
                    ExcludedCodices = _cc.AllCodices.Where(c => !FilterValues.Contains(c.GetFileType()));
                    break;
            }
            return returnExcludedCodices ? ExcludedCodices.ToList() : _cc.AllCodices.Except(ExcludedCodices).ToList();
        }

        //Return List of Codices that Do/Don't match search term
        private HashSet<Codex> GetFilteredCodicesBySearch(string searchterm, bool returnExcludedCodices = true)
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
            ObservableCollection<Filter> Target = include ? IncludedFilters : ExcludedFilters;
            ObservableCollection<Filter> Other = !include ? IncludedFilters : ExcludedFilters;
            //if Filter is unique, remove previous instance of that Filter before adding
            if (filter.Unique)
            {
                Target.Remove(
                    Target.SingleOrDefault(f => f.Type == filter.Type));
            }
            //only add if not yet in Included Tags
            if (!Target.Contains(filter))
            {
                Target.Add(filter);
                Other.Remove(filter);
            }
        }

        public void RemoveFieldFilter(Filter filter)
        {
            IncludedFilters.Remove(filter);
            ExcludedFilters.Remove(filter);
        }
        //------------------------------------//

        private void InitSortingProperties()
        {
            //double check on typos by checking if all property names exist in codex class
            var PossibleSortProptertyNames = typeof(Codex).GetProperties().Select(p => p.Name).ToList();
            if (SortOptions.Select(pair => pair.Value).Except(PossibleSortProptertyNames).Any())
            {
                MessageBox.Show("One of the sort property paths does not exist");
                Logger.log.Error("One of the sort property paths does not exist");
            }
        }
        private void ApplySorting()
        {
            var sortDescr = CollectionViewSource.GetDefaultView(FilteredCodices).SortDescriptions;
            sortDescr.Clear();
            if (string.IsNullOrEmpty(SortProperty)) return;
            sortDescr.Add(new SortDescription(SortProperty, SortDirection));
        }
        private void ApplyFilters()
        {
            FilteredCodices = new(_cc.AllCodices
                .Except(ExcludedCodicesByIncludedTags)
                .Except(ExcludedCodicesByExcludedTags)
                .Except(ExcludedCodicesByFilters)
                .ToList());

            //Also apply filtering to these lists
            RaisePropertyChanged(nameof(Favorites));
            RaisePropertyChanged(nameof(RecentCodices));
            RaisePropertyChanged(nameof(MostOpenedCodices));
            RaisePropertyChanged(nameof(RecentlyAddedCodices));

            FilteredCodices.CollectionChanged += (e, v) => ApplySorting();
            ApplySorting();
        }

        public void ReFilter()
        {
            SetExcludedCodicesByTags();
            SetExcludedCodicesByFilters();
        }
        public void RemoveCodex(Codex c)
        {
            ExcludedCodicesByIncludedTags.Remove(c);
            ExcludedCodicesByExcludedTags.Remove(c);
            ExcludedCodicesByFilters.Remove(c);
            FilteredCodices.Remove(c);
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
            bool ToIncluded = ((CompositeCollection)dropInfo.TargetCollection).Count > 2;

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
