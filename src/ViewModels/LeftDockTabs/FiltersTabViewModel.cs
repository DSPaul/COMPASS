using COMPASS.Commands;
using COMPASS.Models;
using System;
using System.Collections.Generic;

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

        public List<Filter> BooleanFilters { get; } = new()
            {
                new(Filter.FilterType.OfflineSource),
                new(Filter.FilterType.OnlineSource),
                new(Filter.FilterType.PhysicalSource),
                new(Filter.FilterType.Favorite),
            };

        public string SelectedAuthor
        {
            get => null;
            set
            {
                if (!String.IsNullOrEmpty(value))
                {
                    Filter AuthorFilter = new(Filter.FilterType.Author, value);
                    MVM.FilterVM.AddFilter(AuthorFilter, Include);
                }
            }
        }

        public string SelectedPublisher
        {
            get => null;
            set
            {
                if (!String.IsNullOrEmpty(value))
                {
                    Filter PublisherFilter = new(Filter.FilterType.Publisher, value);
                    MVM.FilterVM.AddFilter(PublisherFilter, Include);
                }
            }
        }

        //Selected FileType in FilterTab
        public string SelectedFileType
        {
            get => null;
            set
            {
                if (!String.IsNullOrEmpty(value))
                {
                    Filter FileExtensionFilter = new(Filter.FilterType.FileExtension, value);
                    MVM.FilterVM.AddFilter(FileExtensionFilter, Include);
                }
            }
        }


        //Selected Start and Stop Release Dates
        private DateTime? _startReleaseDate;
        private DateTime? _stopReleaseDate;

        public DateTime? StartReleaseDate
        {
            get => _startReleaseDate;
            set
            {
                SetProperty(ref _startReleaseDate, value);
                if (value != null)
                {
                    Filter startDateFilter = new(Filter.FilterType.StartReleaseDate, value);
                    MVM.FilterVM.AddFilter(startDateFilter, Include);
                }
            }
        }

        public DateTime? StopReleaseDate
        {
            get => _stopReleaseDate;
            set
            {
                SetProperty(ref _stopReleaseDate, value);
                if (value != null)
                {
                    Filter stopDateFilter = new(Filter.FilterType.StopReleaseDate, value);
                    MVM.FilterVM.AddFilter(stopDateFilter, Include);
                }
            }
        }

        //Selected minimum rating
        private int _minRating;
        public int MinRating
        {
            get => _minRating;
            set
            {
                SetProperty(ref _minRating, value);
                if (value > 0 && value < 6)
                {
                    Filter minRatFilter = new(Filter.FilterType.MinimumRating, value);
                    MVM.FilterVM.AddFilter(minRatFilter, Include);
                }
            }
        }

        #endregion

        #region Functions and Commands
        private RelayCommand<Filter> _addSourceFilterCommand;
        public RelayCommand<Filter> AddSourceFilterCommand => _addSourceFilterCommand ??= new(AddSourceFilter);
        public void AddSourceFilter(Filter filter) => MVM.FilterVM.AddFilter(filter, Include);

        private ActionCommand _clearFiltersCommand;
        public ActionCommand ClearFiltersCommand => _clearFiltersCommand ??= new(ClearFilters);
        public void ClearFilters()
        {
            StartReleaseDate = null;
            StopReleaseDate = null;
            MinRating = 0;
            MVM.FilterVM.ClearFilters();
        }
        #endregion
    }
}
