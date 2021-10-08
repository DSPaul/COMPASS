using COMPASS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace COMPASS.Tools
{
    public class FilterHandler : ObservableObject
    {
        readonly Data data;

        //Constuctor
        public FilterHandler(Data CurrentData)
        {
            data = CurrentData;

            ActiveFiles = new List<Codex>(data.AllFiles);

            TagFilteredFiles = new List<Codex>();
            SearchFilteredFiles = new List<Codex>();
            FieldFilteredFiles = new List<Codex>();

            SearchTerm = "";
            ActiveTags = new ObservableCollection<Tag>();
            ActiveTags.CollectionChanged += (e, v) => UpdateTagFilteredFiles();
            ActiveFilters = new ObservableCollection<FilterTag>();
            ActiveFilters.CollectionChanged += (e, v) => UpdateFieldFilteredFiles();
        }

        //Collections
        public ObservableCollection<Tag> ActiveTags { get; set; }
        public ObservableCollection<FilterTag> ActiveFilters { get; set; }

        public List<Codex> TagFilteredFiles { get; set; }
        public List<Codex> SearchFilteredFiles { get; set; }
        public List<Codex> FieldFilteredFiles { get; set; }

        private List<Codex> _activeFiles;
        public List<Codex> ActiveFiles 
        {
            get { return _activeFiles; }
            set { SetProperty(ref _activeFiles, value); }
        }

        #region Properties
        private string searchterm;
        public string SearchTerm
        {
            get { return searchterm; }
            set 
            { 
                SetProperty(ref searchterm, value);

                if (searchterm != "")
                {
                    SearchFilteredFiles = new List<Codex>(data.AllFiles.Where(f => f.Title.IndexOf(SearchTerm, StringComparison.InvariantCultureIgnoreCase) < 0));
                }
                else SearchFilteredFiles = new List<Codex>();

                UpdateActiveFiles();
                
            }
        }

        #endregion

        #region Functions

        public void ClearFilters()
        {
            SearchTerm = "";
            TagFilteredFiles.Clear();
            SearchFilteredFiles.Clear();
            ActiveTags.Clear();
            ActiveFilters.Clear();
            ActiveFiles = new List<Codex>(data.AllFiles);
        }

        //-------------For Tags---------------//
        public void UpdateTagFilteredFiles()
        {
            TagFilteredFiles.Clear();
            List<Tag> ActiveGroups = new List<Tag>();


            //Find all the active groups to filter in
            foreach (Tag t in ActiveTags)
            {
                Tag Group = (Tag)t.GetGroup();
                if (!ActiveGroups.Contains(Group))
                    ActiveGroups.Add(Group);
            }

            //List of Files filtered out in that group
            List<Codex> SingleGroupFilteredFiles = new List<Codex>();
            //Go over every group and filter out files
            foreach (Tag Group in ActiveGroups)
            {
                //Make list with all active tags in that group
                List<Tag> SingleGroupTags = new List<Tag>(ActiveTags.Where(tag => tag.GetGroup() == Group));
                //add childeren of those tags
                for(int i = 0; i<SingleGroupTags.Count(); i++)
                {
                    Tag t = SingleGroupTags[i];
                    foreach(Tag child in t.Items)
                    {
                        if (!SingleGroupTags.Contains(child)) SingleGroupTags.Add(child);
                    } 
                }
                //add parents of those tags, must come AFTER chileren, otherwise childeren of parents are included which is wrong
                for (int i = 0; i < SingleGroupTags.Count(); i++)
                {
                    Tag P = SingleGroupTags[i].GetParent();
                    if (P != null && !P.IsGroup && !SingleGroupTags.Contains(P)) SingleGroupTags.Add(P);
                }
                SingleGroupFilteredFiles = new List<Codex>(data.AllFiles.Where(f => (SingleGroupTags.Intersect(f.Tags)).Count()==0));
                foreach (Codex f in SingleGroupFilteredFiles) if(!TagFilteredFiles.Contains(f)) TagFilteredFiles.Add(f);
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
            FieldFilteredFiles.Clear();
            //List<List<FilterTag>> FieldFilters = new List<List<FilterTag>>(Enum.GetNames(typeof(Enums.FilterType)).Length);
            foreach (Enums.FilterType FT in (Enums.FilterType[])Enum.GetValues(typeof(Enums.FilterType)))
            {
                //FieldFilters.Add(new List<FilterTag>(ActiveFilters.Where(filter => (Enums.FilterType)filter.GetGroup()==FT)));
                List<FilterTag> SingleFieldFilterTags = new List<FilterTag>(ActiveFilters.Where(filter => (Enums.FilterType)filter.GetGroup() == FT));
                List<object> SingleFieldFilters = new List<object>(SingleFieldFilterTags.Select(t => t.FilterData));
                if (SingleFieldFilters.Count == 0) continue;
                List<Codex> SingleFieldFilteredFiles = new List<Codex>();
                switch (FT)
                {
                    case Enums.FilterType.Author:
                        SingleFieldFilteredFiles = new List<Codex>(data.AllFiles.Where(f => !SingleFieldFilters.Contains(f.Author)));
                        break;
                    case Enums.FilterType.Publisher:
                        SingleFieldFilteredFiles = new List<Codex>(data.AllFiles.Where(f => !SingleFieldFilters.Contains(f.Publisher)));
                        break;
                    case Enums.FilterType.StartReleaseDate:
                        SingleFieldFilteredFiles = new List<Codex>(data.AllFiles.Where(f => f.ReleaseDate < (DateTime?)SingleFieldFilters.First()));
                        break;
                    case Enums.FilterType.StopReleaseDate:
                        SingleFieldFilteredFiles = new List<Codex>(data.AllFiles.Where(f => f.ReleaseDate > (DateTime?)SingleFieldFilters.First()));
                        break;
                    case Enums.FilterType.MinimumRating:
                        SingleFieldFilteredFiles = new List<Codex>(data.AllFiles.Where(f => f.Rating < (int)SingleFieldFilters.First()));
                        break;
                }
                FieldFilteredFiles = new List<Codex>(FieldFilteredFiles.Union(SingleFieldFilteredFiles));
            }
            UpdateActiveFiles();
        }
        public void RemoveFieldFilter(FilterTag t)
        {
            ActiveFilters.Remove(t);
        }
        //------------------------------------//
        public void UpdateActiveFiles()
        {
            var tempActiveFiles = new List<Codex>(data.AllFiles);
            var temp1 = tempActiveFiles.Except(SearchFilteredFiles);
            var temp2 = temp1.Except(TagFilteredFiles);
            var temp3 = temp2.Except(FieldFilteredFiles);
            ActiveFiles = new List<Codex>(temp3);
        }

        public void RemoveFile(Codex f)
        {
            TagFilteredFiles.Remove(f);
            SearchFilteredFiles.Remove(f);
            UpdateActiveFiles();
        }

        #endregion
    }
}
