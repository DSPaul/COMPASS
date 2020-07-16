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

            ActiveFiles = new ObservableCollection<MyFile>(data.AllFiles);

            TagFilteredFiles = new ObservableCollection<MyFile>();
            SearchFilteredFiles = new ObservableCollection<MyFile>();
            FieldFilteredFiles = new ObservableCollection<MyFile>();
            TagFilteredFiles.CollectionChanged += (e, v) => Update_ActiveFiles();
            SearchFilteredFiles.CollectionChanged += (e, v) => Update_ActiveFiles();
            FieldFilteredFiles.CollectionChanged += (e, v) => Update_ActiveFiles();

            SearchTerm = "";
            ActiveTags = new ObservableCollection<Tag>();
            ActiveTags.CollectionChanged += (e, v) => UpdateTagFilteredFiles();
            ActiveFilters = new ObservableCollection<FilterTag>();
            ActiveFilters.CollectionChanged += (e, v) => UpdateFieldFilteredFiles();
        }

        //Collections
        public ObservableCollection<Tag> ActiveTags { get; set; }
        public ObservableCollection<FilterTag> ActiveFilters { get; set; }

        public ObservableCollection<MyFile> TagFilteredFiles { get; set; }
        public ObservableCollection<MyFile> SearchFilteredFiles { get; set; }
        public ObservableCollection<MyFile> FieldFilteredFiles { get; set; }

        private ObservableCollection<MyFile> _activeFiles;
        public ObservableCollection<MyFile> ActiveFiles 
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
                    SearchFilteredFiles = new ObservableCollection<MyFile>(data.AllFiles.Where(f => f.Title.IndexOf(SearchTerm, StringComparison.OrdinalIgnoreCase) < 0));
                }
                else SearchFilteredFiles = new ObservableCollection<MyFile>();
                Update_ActiveFiles();
            }
        }

        #endregion

        #region Functions

        public void ClearFilters()
        {
            SearchTerm = "";

            //TagFilteredFiles = new ObservableCollection<MyFile>(data.AllFiles);
            //SearchFilteredFiles = new ObservableCollection<MyFile>(data.AllFiles);
            TagFilteredFiles.Clear();
            SearchFilteredFiles.Clear();
            ActiveTags.Clear();
            ActiveFilters.Clear();
            ActiveFiles = new ObservableCollection<MyFile>(data.AllFiles);
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
            List<MyFile> SingleGroupFilteredFiles = new List<MyFile>();
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
                SingleGroupFilteredFiles = new List<MyFile>(data.AllFiles.Where(f => (SingleGroupTags.Intersect(f.Tags)).Count()==0));
                foreach (MyFile f in SingleGroupFilteredFiles) if(!TagFilteredFiles.Contains(f)) TagFilteredFiles.Add(f);
            }

            Update_ActiveFiles();
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
            //List<List<FilterTag>> FieldFilters = new List<List<FilterTag>>(Enum.GetNames(typeof(Enums.MetaData)).Length);
            foreach (Enums.MetaData MD in (Enums.MetaData[])Enum.GetValues(typeof(Enums.MetaData)))
            {
                //FieldFilters.Add(new List<FilterTag>(ActiveFilters.Where(filter => (Enums.MetaData)filter.GetGroup()==MD)));
                List<FilterTag> SingleFieldFilterTags = new List<FilterTag>(ActiveFilters.Where(filter => (Enums.MetaData)filter.GetGroup() == MD));
                List<string> SingleFieldFilters = new List<string>(SingleFieldFilterTags.Select(t => t.GetFilterTerm()));
                if (SingleFieldFilters.Count == 0) continue;
                List<MyFile> SingleFieldFilteredFiles = new List<MyFile>();
                switch (MD)
                {
                    case Enums.MetaData.Author:
                        SingleFieldFilteredFiles = new List<MyFile>(data.AllFiles.Where(f => !SingleFieldFilters.Contains(f.Author)));
                        break;
                    case Enums.MetaData.Publisher:
                        SingleFieldFilteredFiles = new List<MyFile>(data.AllFiles.Where(f => !SingleFieldFilters.Contains(f.Publisher)));
                        break;
                }
                FieldFilteredFiles = new ObservableCollection<MyFile>(FieldFilteredFiles.Union(SingleFieldFilteredFiles));
            }
            Update_ActiveFiles();
        }
        public void RemoveFieldFilter(FilterTag t)
        {
            ActiveFilters.Remove(t);
        }
        //------------------------------------//
        public void Update_ActiveFiles()
        {
            ActiveFiles = new ObservableCollection<MyFile>(data.AllFiles);
            foreach (MyFile f in SearchFilteredFiles) ActiveFiles.Remove(f);
            foreach (MyFile f in TagFilteredFiles) ActiveFiles.Remove(f);
            foreach (MyFile f in FieldFilteredFiles) ActiveFiles.Remove(f);
            //    ActiveFiles.Clear();
            //    foreach (var p in TagFilteredFiles.Intersect(SearchFilteredFiles))
            //        ActiveFiles.Add(p);
        }

        public void RemoveFile(MyFile f)
        {
            TagFilteredFiles.Remove(f);
            SearchFilteredFiles.Remove(f);
            Update_ActiveFiles();
        }

        #endregion
    }
}
