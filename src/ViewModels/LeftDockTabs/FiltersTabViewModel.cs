using COMPASS.Models;
using COMPASS.ViewModels.Commands;
using System;
using System.Windows.Media;

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
        //Selected FileExtension in FilterTab
        private string _selectedAuthor;
        public string SelectedAuthor
        {
            get => _selectedAuthor;
            set
            {
                SetProperty(ref _selectedAuthor, value);
                Filter AuthorFilter = new(Filter.FilterType.Author, value)
                {
                    Label = "Author:",
                    BackgroundColor = Colors.Orange
                };
                MVM.CollectionVM.AddFieldFilter(AuthorFilter, Include);
            }
        }

        //Selected Publisher in FilterTab
        private string _selectedPublisher;
        public string SelectedPublisher
        {
            get => _selectedPublisher;
            set
            {
                SetProperty(ref _selectedPublisher, value);
                Filter PublisherFilter = new(Filter.FilterType.Publisher, value)
                {
                    Label = "Publisher:",
                    BackgroundColor = Colors.MediumPurple
                };
                MVM.CollectionVM.AddFieldFilter(PublisherFilter, Include);
            }
        }

        //Selected FileExtension in FilterTab
        private string _selectedFileType;
        public string SelectedFileType
        {
            get => _selectedFileType;
            set
            {
                SetProperty(ref _selectedFileType, value);
                Filter FileExtensionFilter = new(Filter.FilterType.FileExtension, value)
                {
                    Label = "File Type:",
                    BackgroundColor = Colors.OrangeRed
                };
                MVM.CollectionVM.AddFieldFilter(FileExtensionFilter, Include);
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
                    Filter startDateFilter = new(Filter.FilterType.StartReleaseDate, value)
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
            get => _stopReleaseDate;
            set
            {
                SetProperty(ref _stopReleaseDate, value);
                if (value != null)
                {
                    Filter stopDateFilter = new(Filter.FilterType.StopReleaseDate, value)
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
        private int _minRating;
        public int MinRating
        {
            get => _minRating;
            set
            {
                SetProperty(ref _minRating, value);
                if (value > 0 && value < 6)
                {
                    Filter minRatFilter = new(Filter.FilterType.MinimumRating, value)
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
            SelectedFileType = null;
            StartReleaseDate = null;
            StopReleaseDate = null;
            MinRating = 0;
            MVM.CollectionVM.ClearFilters();
        }
        #endregion
    }
}
