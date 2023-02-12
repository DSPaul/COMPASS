using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Properties;
using COMPASS.Tools;
using FuzzySharp;
using GongSolutions.Wpf.DragDrop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace COMPASS.ViewModels
{
    public class CollectionViewModel : ViewModelBase, IDropTarget
    {

        //Constuctor
        public CollectionViewModel(CodexCollection currentCollection)
        {
            codexCollection = currentCollection;

            //load sorting from settings
            InitSortingProperties();

            IncludedTags.CollectionChanged += (e, v) => SetExcludedCodicesByTags();
            ExcludedTags.CollectionChanged += (e, v) => SetExcludedCodicesByTags();
            IncludedFilters.CollectionChanged += (e, v) => SetExcludedCodicesByFilters();
            ExcludedFilters.CollectionChanged += (e, v) => SetExcludedCodicesByFilters();

            currentCollection.AllCodices.CollectionChanged += (e, v) => SubscribeToCodexProperties();
            SubscribeToCodexProperties();

            ApplyFilters();
        }

        #region Properties
        private readonly CodexCollection codexCollection;
        private readonly int _itemsShown = 15;
        public int ItemsShown => Math.Min(_itemsShown, FilteredCodices.Count);

        public ObservableCollection<Tag> IncludedTags { get; set; } = new();
        public ObservableCollection<Tag> ExcludedTags { get; set; } = new();
        public ObservableCollection<Filter> IncludedFilters { get; set; } = new();
        public ObservableCollection<Filter> ExcludedFilters { get; set; } = new();

        private HashSet<Codex> ExcludedCodicesByIncludedTags { get; set; } = new();
        private HashSet<Codex> ExcludedCodicesByExcludedTags { get; set; } = new();
        private HashSet<Codex> ExcludedCodicesByFilters { get; set; } = new();

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

        private string _searchTerm = "";
        public string SearchTerm
        {
            get => _searchTerm;
            set => SetProperty(ref _searchTerm, value);
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
            foreach (Codex c in codexCollection.AllCodices)
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
                SingleGroupFilteredCodices = new(codexCollection.AllCodices.Where(f => !SingleGroupTags.Intersect(f.Tags).Any()));

                ExcludedCodicesByIncludedTags = ExcludedCodicesByIncludedTags.Union(SingleGroupFilteredCodices).ToHashSet();
            }

            //Filter out Codices with excluded tags
            //get childeren too, if parent is excluded, so should all the childeren
            List<Tag> AllExcludedTags = Utils.FlattenTree(ExcludedTags).ToList();
            ExcludedCodicesByExcludedTags = new(codexCollection.AllCodices.Where(f => AllExcludedTags.Intersect(f.Tags).Any()));

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
            List<Filter> RelevantFilters = new(Filters.Where(filter => filter.Type == filtertype));

            if (RelevantFilters.Count == 0) return new();

            IEnumerable<Codex> IncludedCodices;
            IEnumerable<Codex> ExcludedCodices;

            if (filtertype == Filter.FilterType.Search)
            {
                IncludedCodices = GetFilteredCodicesBySearch(RelevantFilters.First());
            }
            else
            {
                IncludedCodices = codexCollection.AllCodices.Where(codex => RelevantFilters.Any(filter => filter.Method(codex)));
            }

            ExcludedCodices = codexCollection.AllCodices.Except(IncludedCodices);
            return returnExcludedCodices ? ExcludedCodices.ToList() : IncludedCodices.ToList();
        }

        //Return List of Codices that Do/Don't match search term
        private HashSet<Codex> GetFilteredCodicesBySearch(Filter searchFilter, bool returnExcludedCodices = false)
        {
            string searchterm = (string)searchFilter.FilterValue;

            HashSet<Codex> ExcludedCodicesBySearch = new();
            HashSet<Codex> IncludedCodicesBySearch = new();
            if (!string.IsNullOrEmpty(searchterm))
            {
                //include acronyms
                IncludedCodicesBySearch.UnionWith(codexCollection.AllCodices
                    .Where(f => Fuzz.TokenInitialismRatio(f.Title.ToLowerInvariant(), SearchTerm) > 80));
                //include string fragments
                IncludedCodicesBySearch.UnionWith(codexCollection.AllCodices
                    .Where(f => f.Title.Contains(SearchTerm, StringComparison.InvariantCultureIgnoreCase)));
                //include spelling errors
                //include acronyms
                IncludedCodicesBySearch.UnionWith(codexCollection.AllCodices
                    .Where(f => Fuzz.PartialRatio(f.Title.ToLowerInvariant(), SearchTerm) > 80));

                ExcludedCodicesBySearch = new(codexCollection.AllCodices.Except(IncludedCodicesBySearch));
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
            FilteredCodices = new(codexCollection.AllCodices
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
