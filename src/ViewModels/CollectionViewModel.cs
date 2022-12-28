using COMPASS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using FuzzySharp;
using COMPASS.Tools;
using COMPASS.ViewModels.Commands;
using GongSolutions.Wpf.DragDrop;
using System.Windows;

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
            var PropertyPath = (string)Properties.Settings.Default["SortProperty"];
            var SortDirection = (ListSortDirection)Properties.Settings.Default["SortDirection"];
            SortBy(PropertyPath, SortDirection);

            ExcludedCodicesByTag = new();
            ExcludedCodicesByExcludedTags = new();
            ExcludedCodicesByFilter = new();

            SearchTerm = "";
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
        private int _itemsShown = 15;
        public int ItemsShown => Math.Min(_itemsShown, ActiveFiles.Count);

        //CollectionDirectories
        public ObservableCollection<Tag> ActiveTags { get; set; }
        public ObservableCollection<Tag> DeActiveTags { get; set; }
        public ObservableCollection<FilterTag> ActiveFilters { get; set; }
        public ObservableCollection<FilterTag> DeActiveFilters { get; set; }

        public HashSet<Codex> ExcludedCodicesByTag { get; set; }
        public HashSet<Codex> ExcludedCodicesByExcludedTags { get; set; }
        public HashSet<Codex> ExcludedCodicesByFilter { get; set; }

        private ObservableCollection<Codex> _activeFiles;
        public ObservableCollection<Codex> ActiveFiles 
        {
            get { return _activeFiles; }
            set { SetProperty(ref _activeFiles, value); }
        }

        public ObservableCollection<Codex> Favorites => new (ActiveFiles.Where(c => c.Favorite));
        public List<Codex> RecentCodices => ActiveFiles.OrderByDescending(c => c.LastOpened).ToList().GetRange(0, ItemsShown);
        public List<Codex> MostOpenedCodices => ActiveFiles.OrderByDescending(c => c.OpenedCount).ToList().GetRange(0, ItemsShown);
        public List<Codex> RecentlyAddedCodices => ActiveFiles.OrderByDescending(c => c.DateAdded).ToList().GetRange(0, ItemsShown);

        private string _searchTerm;
        public string SearchTerm
        {
            get { return _searchTerm; }
            set { SetProperty(ref _searchTerm, value); }
        }

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
            foreach (Tag t in ActiveTags)
            {
                Tag Group = (Tag)t.GetGroup();
                ActiveGroups.Add(Group);
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
                    Tag P = SingleGroupTags[i].GetParent();
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
        public RelayCommand<Tag> AddTagFilterCommand => _addTagFilterCommand ??= new(AddTagFilter);
        public void AddTagFilter(Tag t)
        {
            //only add if not yet in activetags
            if (ActiveTags.All(p => p.ID != t.ID))
            {
                ActiveTags.Add(t);
                DeActiveTags.Remove(t);
            }
        }
        public void AddNegTagFilter(Tag t)
        {
            //only add if not yet in deactivetags
            if (DeActiveTags.All(p => p.ID != t.ID))
            {
                DeActiveTags.Add(t);
                ActiveTags.Remove(t);
            }
        }

        private RelayCommand<Tag> _removeFilterCommand;
        public RelayCommand<Tag> RemoveFilterCommand => _removeFilterCommand ??= new(RemoveFilter);
        public void RemoveFilter(Tag t)
        {
            if (!t.GetType().IsSubclassOf(typeof(Tag))) RemoveTagFilter(t);
            else RemoveFieldFilter((FilterTag)t);
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
                //List filter values for current filter type
                List<object> FilterValues = new(
                    ActiveFilters
                    .Where(filter => (Enums.FilterType)filter.GetGroup() == FT)
                    .Select(t => t.FilterValue)
                    );
                List<object> ExcludedFilterValues = new(
                    DeActiveFilters
                    .Where(filter => (Enums.FilterType)filter.GetGroup() == FT)
                    .Select(t => t.FilterValue)
                    );
                //skip iteration if no filters of this type
                if (FilterValues.Count + ExcludedFilterValues.Count == 0) continue;

                bool exclude;
                IEnumerable<Codex> ExcludedCodices = new List<Codex>(); // generic IEnumerable doesn't have constructor so list instead
                switch (FT)
                {
                    case Enums.FilterType.Search:
                        ExcludedCodices = GetSearchFilteredCodices((string)FilterValues.FirstOrDefault())
                            .Concat(GetSearchFilteredCodices((string)ExcludedFilterValues.FirstOrDefault(),false));
                        break;
                    case Enums.FilterType.Author:
                        //exclude codex if intersection between list of authors of codex and list of author filters is empty,
                        //causes problem if list of author filters is empty because then intersection is also empty.
                        //also exclude codex if overlap between authors of the file and excluded author filters, 
                        // same problem with empty intersection if no such filters, so if statements needed
                        if (FilterValues.Count > 0) ExcludedCodices = _cc.AllCodices.Where(f => !FilterValues.Intersect(f.Authors).Any());
                        if (ExcludedFilterValues.Count > 0) ExcludedCodices = ExcludedCodices.Concat(_cc.AllCodices.Where(f => ExcludedFilterValues.Intersect(f.Authors).Any()));
                        break;
                    case Enums.FilterType.Publisher:
                        if (FilterValues.Count > 0) ExcludedCodices = _cc.AllCodices.Where(f => !FilterValues.Contains(f.Publisher));
                        if (ExcludedFilterValues.Count > 0) ExcludedCodices = ExcludedCodices.Concat(_cc.AllCodices.Where(f => ExcludedFilterValues.Contains(f.Publisher)));
                        break;
                    case Enums.FilterType.StartReleaseDate:
                        ExcludedCodices = _cc.AllCodices.Where(f => f.ReleaseDate < (DateTime?)FilterValues.FirstOrDefault())
                        .Concat(_cc.AllCodices.Where(f => f.ReleaseDate >= (DateTime?)ExcludedFilterValues.FirstOrDefault()));
                        break;
                    case Enums.FilterType.StopReleaseDate:
                        ExcludedCodices = _cc.AllCodices.Where(f => f.ReleaseDate > (DateTime?)FilterValues.FirstOrDefault())
                        .Concat(_cc.AllCodices.Where(f => f.ReleaseDate <= (DateTime?)ExcludedFilterValues.FirstOrDefault()));
                        break;
                    case Enums.FilterType.MinimumRating:
                        ExcludedCodices = _cc.AllCodices.Where(f => f.Rating < (int?)FilterValues.FirstOrDefault())
                        .Concat(_cc.AllCodices.Where(f => f.Rating >= (int?)ExcludedFilterValues.FirstOrDefault()));
                        break;
                    case Enums.FilterType.OfflineSource:
                        exclude = (bool)FilterValues.FirstOrDefault();
                        ExcludedCodices = exclude ? _cc.AllCodices.Where(f => f.HasOfflineSource()) : _cc.AllCodices.Where(f => !f.HasOfflineSource());
                        break;
                    case Enums.FilterType.OnlineSource:
                        exclude = (bool)FilterValues.FirstOrDefault();
                        ExcludedCodices = exclude ? _cc.AllCodices.Where(f => f.HasOnlineSource()) :  _cc.AllCodices.Where(f => !f.HasOnlineSource());
                        break;
                    case Enums.FilterType.PhysicalSource:
                        exclude = (bool)FilterValues.FirstOrDefault();
                        ExcludedCodices = exclude ? _cc.AllCodices.Where(f => f.Physically_Owned) : _cc.AllCodices.Where(f => !f.Physically_Owned);
                        break;
                }
                ExcludedCodicesByFilter = ExcludedCodicesByFilter.Union(ExcludedCodices).ToHashSet();
            }
            UpdateActiveFiles();
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
            return returnExcludedCodices?  ExcludedCodicesBySearch : IncludedCodicesBySearch;
        }

        public void AddFieldFilter(FilterTag t, bool include = true)
        {
            ObservableCollection<FilterTag> Target = include ? ActiveFilters : DeActiveFilters;
            ObservableCollection<FilterTag> Other = !include ? ActiveFilters : DeActiveFilters;
            //if Filter is unique, remove previous instance of that Filter before adding
            if (t.Unique)
            {
                Target.Remove(
                    Target.SingleOrDefault(tag => (int)tag.GetGroup() == (int)t.GetGroup()));
            }
            //only add if not yet in activetags
            if (Target.All(p => p.ID != t.ID))
            {
                Target.Add(t);
                Other.Remove(t);
            }
        }

        public void RemoveFieldFilter(FilterTag t)
        {
            ActiveFilters.Remove(t);
            DeActiveFilters.Remove(t);
        }
        //------------------------------------//


        public void SortBy(string PropertyPath, ListSortDirection? SortDirection)
        {
            if (PropertyPath != null && PropertyPath.Length > 0)
            {
                var sortDescr = CollectionViewSource.GetDefaultView(ActiveFiles).SortDescriptions;
                //determine sorting direction, ascending by default
                ListSortDirection lsd = ListSortDirection.Ascending; ;

                if (SortDirection != null) //if direction is given, use that instead
                {
                    lsd = (ListSortDirection)SortDirection;
                }
                else if (sortDescr.Count > 0)
                {
                    if (sortDescr[0].PropertyName == PropertyPath) //if already sorting, change direction
                    {
                        if (sortDescr[0].Direction == ListSortDirection.Ascending) lsd = ListSortDirection.Descending;
                        else lsd = ListSortDirection.Ascending;
                    }
                }

                sortDescr.Clear();
                sortDescr.Add(new SortDescription(PropertyPath, lsd));
                SaveSortDescriptions(PropertyPath, lsd);
            }
        }
        //Single parameter version needed for relaycommand
        public void SortBy(string PropertyPath)
        {
            SortBy(PropertyPath, null);
        }
        private void SaveSortDescriptions(string property, ListSortDirection dir)
        {
            Properties.Settings.Default["SortProperty"] = property;
            Properties.Settings.Default["SortDirection"] = (int)dir;
            Properties.Settings.Default.Save();
        }

        public void ReFilter()
        {
            UpdateTagFilteredFiles();
            UpdateFieldFilteredFiles();
            UpdateActiveFiles();
        }

        public void UpdateActiveFiles()
        {
            //get sorting info
            SortDescription sortDescr;
            if  (CollectionViewSource.GetDefaultView(ActiveFiles).SortDescriptions.Count > 0)
            {
                sortDescr = CollectionViewSource.GetDefaultView(ActiveFiles).SortDescriptions[0];
            }
            SaveSortDescriptions(sortDescr.PropertyName, sortDescr.Direction);

            //compile list of "active" files, which are files that match all the different filters
            ActiveFiles = new (_cc.AllCodices
                .Except(ExcludedCodicesByTag)
                .Except(ExcludedCodicesByExcludedTags)
                .Except(ExcludedCodicesByFilter)
                .ToList());

            //Also apply filtering to these lists
            RaisePropertyChanged(nameof(Favorites));
            RaisePropertyChanged(nameof(RecentCodices));
            RaisePropertyChanged(nameof(MostOpenedCodices));
            RaisePropertyChanged(nameof(RecentlyAddedCodices));

            //reapply sorting, will fail if there aren't any
            try
            {
                CollectionViewSource.GetDefaultView(ActiveFiles).SortDescriptions.Add(sortDescr);
            }
            catch (Exception ex)
            {
                Logger.log.Warn(ex.InnerException);
            }
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
            switch (dropInfo.Data){
                //Move From Treeview
                case TreeViewNode DraggedTVN:
                    if (!DraggedTVN.Tag.IsGroup)
                    {
                        dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                        dropInfo.Effects = DragDropEffects.Copy;
                    }
                    break;
                //Move Filter included/excluded
                case FilterTag ft:
                    //Do filtertag specific stuff here if needed
                //Move Tag between included/excluded
                case Tag t:
                    dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                    dropInfo.Effects = DragDropEffects.Move;
                    break;
            }
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            //Included filter Listbox has extra empty collection to tell the difference
            bool ToExcludeTags = ((CompositeCollection)(dropInfo.TargetCollection)).Count < 3;

            //Tree to Filter Box
            if (dropInfo.Data is TreeViewNode)
            {
                TreeViewNode DraggedTVN = (TreeViewNode)dropInfo.Data;
                if (ToExcludeTags) { AddNegTagFilter(DraggedTVN.Tag); }
                else { AddTagFilter(DraggedTVN.Tag); }
            }

            //Move between included/excluded
            if (dropInfo.Data is Tag)
            {
                if (dropInfo.Data is FilterTag)
                {
                    FilterTag DraggedTag = (FilterTag)dropInfo.Data;
                    if (ToExcludeTags) { AddFieldFilter(DraggedTag, false); }
                    else { AddFieldFilter(DraggedTag); }
                }
                else
                {
                    Tag DraggedTag = (Tag)dropInfo.Data;
                    if (ToExcludeTags) { AddNegTagFilter(DraggedTag); }
                    else { AddTagFilter(DraggedTag); }
                } 
            }
        }
        #endregion
    }
}
