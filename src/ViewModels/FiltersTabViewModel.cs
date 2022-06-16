using COMPASS.Models;
using COMPASS.ViewModels.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using static COMPASS.Tools.Enums;

namespace COMPASS.ViewModels
{
    public class FiltersTabViewModel : BaseViewModel
    {
        public FiltersTabViewModel() :base()
        {
            ChangeOnlineFilterCommand = new(ChangeOnlineFilter);
            ChangeOfflineFilterCommand = new(ChangeOfflineFilter);
            ChangePhysicalFilterCommand = new(ChangePhysicalFilter);
            ClearFiltersCommand = new(ClearFilters);
        }

        #region Properties
        //Selected Autor in FilterTab
        private string selectedAuthor;
        public string SelectedAuthor
        {
            get { return selectedAuthor; }
            set
            {
                SetProperty(ref selectedAuthor, value);
                FilterTag AuthorTag = new(MVM.FilterHandler.ActiveFilters, FilterType.Author, value) 
                    { Content = "Author: " + value, BackgroundColor = Colors.Orange };
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
                FilterTag PublTag = new(MVM.FilterHandler.ActiveFilters, FilterType.Publisher, value) 
                    { Content = "Publisher: " + value, BackgroundColor = Colors.MediumPurple };
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
                if (value != null)
                {
                    FilterTag startDateTag = new(MVM.FilterHandler.ActiveFilters, FilterType.StartReleaseDate, value)
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
                if (value != null)
                {
                    FilterTag stopDateTag = new(MVM.FilterHandler.ActiveFilters, FilterType.StopReleaseDate, value)
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
                if (value > 0 && value < 6)
                {
                    FilterTag minRatTag = new(MVM.FilterHandler.ActiveFilters, FilterType.MinimumRating, value)
                    { Content = "At least " + value + " stars", BackgroundColor = Colors.Goldenrod };
                    //Remove existing minimum rating, replacing it
                    MVM.FilterHandler.ActiveFilters.Remove(MVM.FilterHandler.ActiveFilters.Where(filter => (FilterType)filter.GetGroup() == FilterType.MinimumRating).FirstOrDefault());
                    MVM.FilterHandler.ActiveFilters.Add(minRatTag);
                }
            }
        }

        #endregion

        #region Functions and Commands
        public RelayCommand<Tuple<bool, bool>> ChangeOnlineFilterCommand { get; init; }
        public void ChangeOnlineFilter(Tuple<bool, bool> parameters)
        {
            ChangeSourceFilter(FilterType.OnlineSource, "Available Online", parameters.Item1, parameters.Item2);
        }

        public RelayCommand<Tuple<bool, bool>> ChangeOfflineFilterCommand { get; init; }
        public void ChangeOfflineFilter(Tuple<bool, bool> parameters)
        {
            ChangeSourceFilter(FilterType.OfflineSource, "Available Offline", parameters.Item1, parameters.Item2);
        }

        public RelayCommand<Tuple<bool, bool>> ChangePhysicalFilterCommand { get; init; }
        public void ChangePhysicalFilter(Tuple<bool, bool> parameters)
        {
            ChangeSourceFilter(FilterType.PhysicalSource, "Physicaly Owned", parameters.Item1, parameters.Item2);
        }

        public void ChangeSourceFilter(FilterType ft, string text, bool addFilter, bool invert)
        {
            //remove old filter, either to remove or replace
            MVM.FilterHandler.ActiveFilters.Remove(MVM.FilterHandler.ActiveFilters.Where(filter => (FilterType)filter.GetGroup() == ft).FirstOrDefault());

            if (invert) text = "NOT: " + text;

            if (addFilter)
            {
                FilterTag t = new(MVM.FilterHandler.ActiveFilters, ft, invert)
                { Content = text, BackgroundColor = Colors.Violet };
                //Remove existing end date, replacing it
                MVM.FilterHandler.ActiveFilters.Add(t);
            }
        }

        public ActionCommand ClearFiltersCommand { get; init; }
        public void ClearFilters()
        {
            SelectedAuthor = null;
            SelectedPublisher = null;
            StartReleaseDate = null;
            StopReleaseDate = null;
            MinRating = 0;
            MVM.FilterHandler.ActiveFilters.Clear();
        }
        #endregion
    }
}
