﻿using COMPASS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;

namespace COMPASS.Tools
{
    public class FilterHandler : ObservableObject
    {
        readonly CodexCollection cc;

        //Constuctor
        public FilterHandler(CodexCollection CurrentCollection)
        {
            cc = CurrentCollection;

            ActiveFiles = new ObservableCollection<Codex>(cc.AllFiles);

            //load sorting from settings
            var PropertyPath = (string)Properties.Settings.Default["SortProperty"];
            var SortDirection = (ListSortDirection) Properties.Settings.Default["SortDirection"];
            SortBy(PropertyPath,SortDirection);

            ExcludedCodicesByTag = new HashSet<Codex>();
            ExcludedCodicesBySearch = new HashSet<Codex>();
            ExcludedCodicesByFilter = new HashSet<Codex>();

            SearchTerm = "";
            ActiveTags = new ObservableCollection<Tag>();
            ActiveTags.CollectionChanged += (e, v) => UpdateTagFilteredFiles();
            ActiveFilters = new ObservableCollection<FilterTag>();
            ActiveFilters.CollectionChanged += (e, v) => UpdateFieldFilteredFiles();
            SearchFilters = new ObservableCollection<FilterTag>();
        }

        #region Properties
        //Collections
        public ObservableCollection<Tag> ActiveTags { get; set; }
        public ObservableCollection<FilterTag> ActiveFilters { get; set; }
        public ObservableCollection<FilterTag> SearchFilters { get; set; }

        public HashSet<Codex> ExcludedCodicesByTag { get; set; }
        public HashSet<Codex> ExcludedCodicesBySearch { get; set; }
        public HashSet<Codex> ExcludedCodicesByFilter { get; set; }

        private ObservableCollection<Codex> _activeFiles;
        public ObservableCollection<Codex> ActiveFiles 
        {
            get { return _activeFiles; }
            set { SetProperty(ref _activeFiles, value); }
        }

        private string searchterm;
        public string SearchTerm
        {
            get { return searchterm; }
            set { SetProperty(ref searchterm, value); }
        }

        #endregion

        #region Functions

        public void ClearFilters()
        {
            SearchTerm = "";
            UpdateSearchFilteredFiles("");
            ExcludedCodicesByTag.Clear();
            ActiveTags.Clear();
            ActiveFilters.Clear();
            ActiveFiles = new ObservableCollection<Codex>(cc.AllFiles);
        }

        //-------------For Tags---------------//
        public void UpdateTagFilteredFiles()
        {
            ExcludedCodicesByTag.Clear();
            HashSet<Tag> ActiveGroups = new HashSet<Tag>();


            //Find all the active groups to filter in
            foreach (Tag t in ActiveTags)
            {
                Tag Group = (Tag)t.GetGroup();
                ActiveGroups.Add(Group);
            }

            //List of Files filtered out in that group
            HashSet<Codex> SingleGroupFilteredFiles = new HashSet<Codex>();
            //Go over every group and filter out files
            foreach (Tag Group in ActiveGroups)
            {
                //Make list with all active tags in that group, including childeren
                List<Tag> SingleGroupTags = Utils.FlattenTree(ActiveTags.Where(tag => tag.GetGroup() == Group)).ToList();
                //add parents of those tags, must come AFTER chileren, otherwise childeren of parents are included which is wrong
                for (int i = 0; i < SingleGroupTags.Count(); i++)
                {
                    Tag P = SingleGroupTags[i].GetParent();
                    if (P != null && !P.IsGroup && !SingleGroupTags.Contains(P)) SingleGroupTags.Add(P);
                }
                SingleGroupFilteredFiles = new HashSet<Codex>(cc.AllFiles.Where(f => (SingleGroupTags.Intersect(f.Tags)).Count()==0));
                
                ExcludedCodicesByTag = ExcludedCodicesByTag.Union(SingleGroupFilteredFiles).ToHashSet();
            }

            UpdateActiveFiles();
        } 

        public void AddTagFilter(Tag t)
        {
            //only add if not yet in activetags
            if (ActiveTags.All(p => p.ID != t.ID))
            {
                ActiveTags.Add(t);
            }
        }

        public void RemoveTagFilter(Tag t)
        {
            ActiveTags.Remove(t);
        }
        //------------------------------------//

        //-------------For Filters------------//
        public void UpdateFieldFilteredFiles()
        {
            ExcludedCodicesByFilter.Clear();
            //List<List<FilterTag>> FieldFilters = new List<List<FilterTag>>(Enum.GetNames(typeof(Enums.FilterType)).Length);

            //enumerate over all filter types
            foreach (Enums.FilterType FT in (Enums.FilterType[])Enum.GetValues(typeof(Enums.FilterType)))
            {
                //FieldFilters.Add(new List<FilterTag>(ActiveFilters.Where(filter => (Enums.FilterType)filter.GetGroup()==FT)));

                //List filter values for current filter type
                List<object> FilterValues = new List<object>(
                    ActiveFilters
                    .Where(filter => (Enums.FilterType)filter.GetGroup() == FT)
                    .Select(t => t.FilterValue)
                    );
                //skip iteration if no filters of this type
                if (FilterValues.Count == 0) continue;

                //true if filter is inverted => Codices that match filter should NOT be displayed
                bool invert = false;
                IEnumerable<Codex> ExcludedCodices = null;
                switch (FT)
                {
                    case Enums.FilterType.Search:
                        //handled by UpdateSearchFilteredFiles
                        break;
                    case Enums.FilterType.Author:
                        ExcludedCodices = cc.AllFiles.Where(f => !FilterValues.Contains(f.Author));
                        break;
                    case Enums.FilterType.Publisher:
                        ExcludedCodices = cc.AllFiles.Where(f => !FilterValues.Contains(f.Publisher));
                        break;
                    case Enums.FilterType.StartReleaseDate:
                        ExcludedCodices = cc.AllFiles.Where(f => f.ReleaseDate < (DateTime?)FilterValues.First());
                        break;
                    case Enums.FilterType.StopReleaseDate:
                        ExcludedCodices = cc.AllFiles.Where(f => f.ReleaseDate > (DateTime?)FilterValues.First());
                        break;
                    case Enums.FilterType.MinimumRating:
                        ExcludedCodices = cc.AllFiles.Where(f => f.Rating < (int)FilterValues.First());
                        break;
                    case Enums.FilterType.OfflineSource:
                        invert = (bool)FilterValues.First();
                        ExcludedCodices = cc.AllFiles.Where(f => !f.HasOfflineSource());
                        break;
                    case Enums.FilterType.OnlineSource:
                        invert = (bool)FilterValues.First();
                        ExcludedCodices = cc.AllFiles.Where(f => !f.HasOnlineSource());
                        break;
                    case Enums.FilterType.PhysicalSource:
                        invert = (bool)FilterValues.First();
                        ExcludedCodices = cc.AllFiles.Where(f => !f.Physically_Owned);
                        break;
                }
                if (invert) ExcludedCodices = cc.AllFiles.Except(ExcludedCodices);
                ExcludedCodicesByFilter = ExcludedCodicesByFilter.Union(ExcludedCodices).ToHashSet();
            }
            UpdateActiveFiles();
        }
        public void RemoveFieldFilter(FilterTag t)
        {
            if ((Enums.FilterType)t.GetGroup() == Enums.FilterType.Search)
            {
                SearchFilters.Remove(t);
                UpdateSearchFilteredFiles("");
            }

            else ActiveFilters.Remove(t);
        }
        //------------------------------------//

        //------------ For Search-------------//
        public void UpdateSearchFilteredFiles(string searchterm)
        {
            SearchFilters.Clear();
            if (searchterm != "")
            {
                ExcludedCodicesBySearch = new HashSet<Codex>(cc.AllFiles.Where(f => f.Title.IndexOf(SearchTerm, StringComparison.InvariantCultureIgnoreCase) < 0));
                FilterTag SearchTag = new FilterTag(SearchFilters, Enums.FilterType.Search, searchterm) { Content = "Search: " + SearchTerm, BackgroundColor = Colors.Salmon };
                SearchFilters.Add(SearchTag);
            }
            else ExcludedCodicesBySearch.Clear();

            UpdateActiveFiles();
        }
        //------------------------------------//


        public void SortBy(string PropertyPath, ListSortDirection? SortDirection = null)
        {
            if (PropertyPath != null && PropertyPath.Length > 0)
            {
                var sortDescr = CollectionViewSource.GetDefaultView(ActiveFiles).SortDescriptions;
                //determine sorting direction, ascending by default
                ListSortDirection lsd = ListSortDirection.Ascending; ;

                if(SortDirection != null) //if direction is given, use that instead
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
                sortDescr.Add(new SortDescription(PropertyPath,lsd));
                SaveSortDescriptions(PropertyPath,lsd);
            }
        }

        public void SaveSortDescriptions(string property, ListSortDirection dir)
        {
            Properties.Settings.Default["SortProperty"] = property;
            Properties.Settings.Default["SortDirection"] = (int)dir;
            Properties.Settings.Default.Save();
        }

        public void ReFilter()
        {
            UpdateTagFilteredFiles();
            UpdateFieldFilteredFiles();
            UpdateSearchFilteredFiles(searchterm);
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
            ActiveFiles = new ObservableCollection<Codex>(cc.AllFiles
                .Except(ExcludedCodicesBySearch)
                .Except(ExcludedCodicesByTag)
                .Except(ExcludedCodicesByFilter)
                .ToList());

            //reapply sorting, will fail if there aren't any
            try
            {
                CollectionViewSource.GetDefaultView(ActiveFiles).SortDescriptions.Add(sortDescr);
            }
            catch { }
        }

        public void RemoveFile(Codex f)
        {
            ExcludedCodicesByTag.Remove(f);
            ExcludedCodicesBySearch.Remove(f);
            ExcludedCodicesByFilter.Remove(f);
            ActiveFiles.Remove(f);
        }

        #endregion
    }
}