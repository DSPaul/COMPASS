using COMPASS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COMPASS.Tools
{
    public class FilterHandler : ObservableObject
    {
        readonly Data data;

        //Constuctor
        public FilterHandler(Data CurrentData)
        {
            data = CurrentData;
            SearchTerm = "";
            TagFilteredFiles = new ObservableCollection<MyFile>(data.AllFiles);
            SearchFilteredFiles = new ObservableCollection<MyFile>(data.AllFiles);
            ActiveFiles = new ObservableCollection<MyFile>(data.AllFiles);
            ActiveTags = new ObservableCollection<Tag>();
        }

        //Collections
        public ObservableCollection<Tag> ActiveTags { get; set; }

        public ObservableCollection<MyFile> TagFilteredFiles { get; set; }
        public ObservableCollection<MyFile> SearchFilteredFiles { get; set; }
        public ObservableCollection<MyFile> ActiveFiles { get; set; }

        //Properties
        private string searchterm;
        public string SearchTerm
        {
            get { return searchterm; }
            set 
            { 
                SetProperty(ref searchterm, value);

                if (searchterm != "")
                {
                    SearchFilteredFiles = new ObservableCollection<MyFile>(data.AllFiles.Where(f => f.Title.IndexOf(SearchTerm, StringComparison.OrdinalIgnoreCase) >= 0));
                }
                else SearchFilteredFiles = new ObservableCollection<MyFile>(data.AllFiles);
                Update_ActiveFiles();
            }
        }       

        //Funcitons
        public void ClearFilters()
        {
            SearchTerm = "";

            TagFilteredFiles = new ObservableCollection<MyFile>(data.AllFiles);
            SearchFilteredFiles = new ObservableCollection<MyFile>(data.AllFiles);
            ActiveTags.Clear();
            Update_ActiveFiles();
        }

        public void AddTagFilter(Tag t)
        {
            if (ActiveTags.All(p => p.ID != t.ID))
            {
                ActiveTags.Add(t);
            }
            foreach (MyFile f in data.AllFiles)
            {
                if (!f.Tags.Contains(t) && TagFilteredFiles.Contains(f)) TagFilteredFiles.Remove(f);
            }
            Update_ActiveFiles();
        }

        public void RemoveTagFilter(Tag t)
        {
            ActiveTags.Remove(t);
            TagFilteredFiles.Clear();
            foreach (MyFile f in data.AllFiles)
            {
                if (ActiveTags.All(i => f.Tags.Contains(i)))
                    TagFilteredFiles.Add(f);
            }
            Update_ActiveFiles();
        }

        public void Update_ActiveFiles()
        {
            if (ActiveFiles == null) return;

            //if (SearchFilteredFiles.Count == data.AllTags.Count) 
            //{
            //    ActiveFiles = new ObservableCollection<MyFile>(TagFilteredFiles);
            //    return;
            //}
            //if (TagFilteredFiles.Count == data.AllTags.Count)
            //{
            //    ActiveFiles = new ObservableCollection<MyFile>(TagFilteredFiles);
            //    return;
            //}
            ActiveFiles.Clear();
            foreach (var p in TagFilteredFiles.Intersect(SearchFilteredFiles))
                ActiveFiles.Add(p);
        }

        public void RemoveFile(MyFile f)
        {
            TagFilteredFiles.Remove(f);
            SearchFilteredFiles.Remove(f);
            Update_ActiveFiles();
        }
    }
}
