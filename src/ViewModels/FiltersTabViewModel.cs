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
    public class FiltersTabViewModel : ViewModelBase
    {
        public FiltersTabViewModel() :base(){}

        #region Properties
        //Selected Autor in FilterTab
        private string selectedAuthor;
        public string SelectedAuthor
        {
            get { return selectedAuthor; }
            set
            {
                SetProperty(ref selectedAuthor, value);
                FilterTag AuthorTag = new(FilterType.Author, value) 
                { 
                    Label = "Author:", 
                    BackgroundColor = Colors.Orange
                };
                MVM.CollectionVM.AddFieldFilter(AuthorTag);
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
                FilterTag PublTag = new(FilterType.Publisher, value) 
                { 
                    Label = "Publisher:", 
                    BackgroundColor = Colors.MediumPurple 
                };
                MVM.CollectionVM.AddFieldFilter(PublTag);
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
                    FilterTag startDateTag = new(FilterType.StartReleaseDate, value)
                    { 
                        Label = "After:", 
                        BackgroundColor = Colors.DeepSkyBlue,
                        Unique = true
                    };
                    MVM.CollectionVM.AddFieldFilter(startDateTag);
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
                    FilterTag stopDateTag = new(FilterType.StopReleaseDate, value)
                    {
                        Label = "Before:",
                        BackgroundColor = Colors.DeepSkyBlue,
                        Unique = true
                    };
                    MVM.CollectionVM.AddFieldFilter(stopDateTag);
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
                    FilterTag minRatTag = new(FilterType.MinimumRating, value)
                    { 
                        Label = "At least", 
                        Suffix = "stars", 
                        BackgroundColor = Colors.Goldenrod,
                        Unique = true
                    };
                    MVM.CollectionVM.AddFieldFilter(minRatTag);
                }
            }
        }

        #endregion

        #region Functions and Commands
        private RelayCommand<Tuple<bool, bool>> _changeOnlineFileterCommand;
        public RelayCommand<Tuple<bool, bool>> ChangeOnlineFilterCommand => _changeOnlineFileterCommand ??= new(ChangeOnlineFilter);
        public void ChangeOnlineFilter(Tuple<bool, bool> parameters)
        {
            ChangeSourceFilter(FilterType.OnlineSource, "Available Online", parameters.Item1, parameters.Item2);
        }

        private RelayCommand<Tuple<bool, bool>> _changeOfflineFilterCommand;
        public RelayCommand<Tuple<bool, bool>> ChangeOfflineFilterCommand => _changeOfflineFilterCommand ??= new(ChangeOfflineFilter);
        public void ChangeOfflineFilter(Tuple<bool, bool> parameters)
        {
            ChangeSourceFilter(FilterType.OfflineSource, "Available Offline", parameters.Item1, parameters.Item2);
        }

        private RelayCommand<Tuple<bool, bool>> _changePhysicalFilterCommand;
        public RelayCommand<Tuple<bool, bool>> ChangePhysicalFilterCommand => _changePhysicalFilterCommand ??= new(ChangePhysicalFilter);
        public void ChangePhysicalFilter(Tuple<bool, bool> parameters)
        {
            ChangeSourceFilter(FilterType.PhysicalSource, "Physicaly Owned", parameters.Item1, parameters.Item2);
        }

        public void ChangeSourceFilter(FilterType ft, string text, bool addFilter, bool invert)
        {
            //remove old filter, either to remove or replace
            MVM.CollectionVM.ActiveFilters.Remove(MVM.CollectionVM.ActiveFilters.Where(filter => (FilterType)filter.GetGroup() == ft).FirstOrDefault());

            if (invert) text = "NOT: " + text;

            if (addFilter)
            {
                FilterTag t = new(ft, invert)
                { Content = text, BackgroundColor = Colors.Violet };
                //Remove existing end date, replacing it
                MVM.CollectionVM.ActiveFilters.Add(t);
            }
        }

        private ActionCommand _clearFiltersCommand;
        public ActionCommand ClearFiltersCommand => _clearFiltersCommand ??= new(ClearFilters);
        public void ClearFilters()
        {
            SelectedAuthor = null;
            SelectedPublisher = null;
            StartReleaseDate = null;
            StopReleaseDate = null;
            MinRating = 0;
            MVM.CollectionVM.ActiveFilters.Clear();
        }
        #endregion
    }
}
