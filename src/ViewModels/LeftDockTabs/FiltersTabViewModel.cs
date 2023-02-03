using COMPASS.Models;
using COMPASS.ViewModels.Commands;
using System;
using System.Windows.Media;
using static COMPASS.Tools.Enums;

namespace COMPASS.ViewModels
{
    public class FiltersTabViewModel : ViewModelBase
    {
        public FiltersTabViewModel() : base() { }

        private bool _include = true;
        public bool Include 
        { 
            get => _include; 
            set => SetProperty(ref _include, value); 
        }

        #region Filters
        //Selected Autor in FilterTab
        private string selectedAuthor;
        public string SelectedAuthor
        {
            get { return selectedAuthor; }
            set
            {
                SetProperty(ref selectedAuthor, value);
                Filter AuthorFilter = new(FilterType.Author, value)
                {
                    Label = "Author:",
                    BackgroundColor = Colors.Orange
                };
                MVM.CollectionVM.AddFieldFilter(AuthorFilter, Include);
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
                Filter PublisherFilter = new(FilterType.Publisher, value)
                {
                    Label = "Publisher:",
                    BackgroundColor = Colors.MediumPurple
                };
                MVM.CollectionVM.AddFieldFilter(PublisherFilter, Include);
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
                    Filter startDateFilter = new(FilterType.StartReleaseDate, value)
                    {
                        Label = "After:",
                        BackgroundColor = Colors.DeepSkyBlue,
                        Unique = true
                    };
                    MVM.CollectionVM.AddFieldFilter(startDateFilter, Include);
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
                    Filter stopDateFilter = new(FilterType.StopReleaseDate, value)
                    {
                        Label = "Before:",
                        BackgroundColor = Colors.DeepSkyBlue,
                        Unique = true
                    };
                    MVM.CollectionVM.AddFieldFilter(stopDateFilter, Include);
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
                    Filter minRatFilter = new(FilterType.MinimumRating, value)
                    {
                        Label = "At least",
                        Suffix = "stars",
                        BackgroundColor = Colors.Goldenrod,
                        Unique = true
                    };
                    MVM.CollectionVM.AddFieldFilter(minRatFilter, Include);
                }
            }
        }

        #endregion

        #region Functions and Commands
        private RelayCommand<Filter> _addSourceFilterCommand;
        public RelayCommand<Filter> AddSourceFilterCommand => _addSourceFilterCommand ??= new(AddSourceFilter);
        public void AddSourceFilter(Filter filter) => MVM.CollectionVM.AddFieldFilter(filter, Include);

        private ActionCommand _clearFiltersCommand;
        public ActionCommand ClearFiltersCommand => _clearFiltersCommand ??= new(ClearFilters);
        public void ClearFilters()
        {
            SelectedAuthor = null;
            SelectedPublisher = null;
            StartReleaseDate = null;
            StopReleaseDate = null;
            MinRating = 0;
            MVM.CollectionVM.ClearFilters();
        }
        #endregion
    }
}
