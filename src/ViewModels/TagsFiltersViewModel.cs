using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.ViewModels.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using static COMPASS.Tools.Enums;

namespace COMPASS.ViewModels
{
    public class TagsFiltersViewModel : DealsWithTreeviews
    {
        public TagsFiltersViewModel(): base()
        {
            ChangeOnlineFilterCommand = new RelayCommand<Tuple<bool, bool>>(ChangeOnlineFilter);
            ChangeOfflineFilterCommand = new RelayCommand<Tuple<bool, bool>>(ChangeOfflineFilter);
            ChangePhysicalFilterCommand = new RelayCommand<Tuple<bool, bool>>(ChangePhysicalFilter);
            EditTagCommand = new ActionCommand(EditTag);
            DeleteTagCommand = new ActionCommand(DeleteTag);
            ClearFiltersCommand = new ActionCommand(ClearFilters);
        }



        #region Properties
        //selected Tag in Treeview
        public Tag SelectedTag
        {
            get
            {
                foreach (TreeViewNode t in AllTreeViewNodes)
                {
                    if (t.Selected) return t.Tag;
                }
                return null;
            }
            set
            {
                foreach (TreeViewNode t in AllTreeViewNodes)
                {
                    if (t.Tag == value) t.Selected = true;
                    else t.Selected = false;
                }
            }
        }

        //Selected Autor in FilterTab
        private string selectedAuthor;
        public string SelectedAuthor
        {
            get { return selectedAuthor; }
            set
            {
                SetProperty(ref selectedAuthor, value);
                FilterTag AuthorTag = new FilterTag(MVM.FilterHandler.ActiveFilters,FilterType.Author,value) { Content = "Author: " + value, BackgroundColor = Colors.Orange };
                MVM.FilterHandler.ActiveFilters.Add(AuthorTag);
            }
        }

        //Selected Publisher in FilterTab
        private string selectedPublisher;
        public string SelectedPublisher
        {
            get { return selectedPublisher; }
            set
            {
                SetProperty(ref selectedPublisher, value);
                FilterTag PublTag = new FilterTag(MVM.FilterHandler.ActiveFilters,FilterType.Publisher,value) { Content = "Publisher: " + value, BackgroundColor = Colors.MediumPurple };
                MVM.FilterHandler.ActiveFilters.Add(PublTag);
            }
        }

        //Selected Start and Stop Release Dates
        private DateTime? startReleaseDate;
        private DateTime? stopReleaseDate;

        public DateTime? StartReleaseDate
        {
            get { return startReleaseDate; }
            set
            {
                SetProperty(ref startReleaseDate, value);
                if(value != null)
                {
                    FilterTag startDateTag = new FilterTag(MVM.FilterHandler.ActiveFilters, FilterType.StartReleaseDate, value) 
                    { Content = "After: " + value.Value.Date.ToShortDateString(), BackgroundColor = Colors.DeepSkyBlue };
                    //Remove existing start date, replacing it
                    MVM.FilterHandler.ActiveFilters.Remove(MVM.FilterHandler.ActiveFilters.Where(filter => (FilterType)filter.GetGroup() == FilterType.StartReleaseDate).FirstOrDefault());
                    MVM.FilterHandler.ActiveFilters.Add(startDateTag);
                }
            }
        }

        public DateTime? StopReleaseDate
        {
            get { return stopReleaseDate; }
            set
            {
                SetProperty(ref stopReleaseDate, value);
                if(value!= null)
                {
                    FilterTag stopDateTag = new FilterTag(MVM.FilterHandler.ActiveFilters, FilterType.StopReleaseDate,value) 
                    { Content = "Before: " + value.Value.Date.ToShortDateString(), BackgroundColor = Colors.DeepSkyBlue };
                    //Remove existing end date, replacing it
                    MVM.FilterHandler.ActiveFilters.Remove(MVM.FilterHandler.ActiveFilters.Where(filter => (FilterType)filter.GetGroup() == FilterType.StopReleaseDate).FirstOrDefault());
                    MVM.FilterHandler.ActiveFilters.Add(stopDateTag);
                }
            }
        }

        //Selected minimum rating
        private int minRating;
        public int MinRating
        {
            get { return minRating; }
            set
            {
                SetProperty(ref minRating, value);
                if(value>0 && value< 6)
                {
                    FilterTag minRatTag = new FilterTag(MVM.FilterHandler.ActiveFilters, FilterType.MinimumRating, value) 
                    { Content = "At least " + value + " stars", BackgroundColor = Colors.Goldenrod };
                    //Remove existing minimum rating, replacing it
                    MVM.FilterHandler.ActiveFilters.Remove(MVM.FilterHandler.ActiveFilters.Where(filter => (FilterType)filter.GetGroup() == FilterType.MinimumRating).FirstOrDefault());
                    MVM.FilterHandler.ActiveFilters.Add(minRatTag);
                }
            }
        }

        //Tag for Context Menu
        public Tag Context;

        #endregion

        #region Functions and Commands
        public RelayCommand<Tuple<bool, bool>> ChangeOnlineFilterCommand { get; private set; }
        public void ChangeOnlineFilter(Tuple<bool, bool> parameters)
        {
            ChangeSourceFilter(FilterType.OnlineSource,"Available Online",parameters.Item1,parameters.Item2);
        }

        public RelayCommand<Tuple<bool, bool>> ChangeOfflineFilterCommand { get; private set; }
        public void ChangeOfflineFilter(Tuple<bool, bool> parameters)
        {
            ChangeSourceFilter(FilterType.OfflineSource,"Available Offline", parameters.Item1, parameters.Item2);
        }
        
        public RelayCommand<Tuple<bool, bool>> ChangePhysicalFilterCommand { get; private set; }
        public void ChangePhysicalFilter(Tuple<bool, bool> parameters)
        {
            ChangeSourceFilter(FilterType.PhysicalSource,"Physicaly Owned", parameters.Item1, parameters.Item2);
        }

        public void ChangeSourceFilter(FilterType ft, string text, bool addFilter, bool invert)
        {
            //remove old filter, either to remove or replace
            MVM.FilterHandler.ActiveFilters.Remove(MVM.FilterHandler.ActiveFilters.Where(filter => (FilterType)filter.GetGroup() == ft).FirstOrDefault());

            if (invert) text = "NOT: " + text;

            if (addFilter)
            {
                FilterTag t = new FilterTag(MVM.FilterHandler.ActiveFilters, ft, invert)
                { Content = text, BackgroundColor = Colors.Violet };
                //Remove existing end date, replacing it
                MVM.FilterHandler.ActiveFilters.Add(t);
            }
        }

        //-------------------For Tags Tab ---------------------//
        public ActionCommand EditTagCommand { get; private set; }
        public void EditTag()
        {
            if (Context != null)
            {
                MVM.CurrentEditViewModel = new TagEditViewModel(Context);
                TagPropWindow tpw = new TagPropWindow((TagEditViewModel)MVM.CurrentEditViewModel);
                tpw.ShowDialog();
                tpw.Topmost = true;
            }
        }

        public ActionCommand DeleteTagCommand { get; private set; }
        public void DeleteTag()
        {
            //tag to delete is context, because DeleteTag is called from context menu
            if (Context == null) return;
            MVM.CurrentCollection.DeleteTag(Context);
            MVM.FilterHandler.RemoveTagFilter(Context);

            //Go over all files and remove the tag from tag list
            foreach (var f in MVM.CurrentCollection.AllFiles)
            {
                f.Tags.Remove(Context);
            }
            MVM.Refresh();

            //SelectedTag = null;
        }
        //-----------------------------------------------------//

        //----------------For Filters Tab---------------------//
        public ActionCommand ClearFiltersCommand { get; private set; }
        public void ClearFilters()
        {
            SelectedAuthor = null;
            SelectedPublisher = null;
            StartReleaseDate = null;
            StopReleaseDate = null;
            MinRating = 0;
            MVM.FilterHandler.ActiveFilters.Clear();
        }
        //-----------------------------------------------------//
        #endregion
    }
}
