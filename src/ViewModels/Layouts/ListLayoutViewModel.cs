using System.ComponentModel;

namespace COMPASS.ViewModels
{
    public class ListLayoutViewModel : LayoutViewModel
    {
        public ListLayoutViewModel() : base()
        {
            LayoutType = Layout.List;
        }

        public override bool DoVirtualization => Properties.Settings.Default.DoVirtualizationList && MainViewModel.CollectionVM.CurrentCollection.AllCodices.Count > Properties.Settings.Default.VirtualizationThresholdList;

        #region ViewOptions

        private bool _showTitle = true;
        public bool ShowTitle
        {
            get => _showTitle;
            set => SetProperty(ref _showTitle, value);
        }

        [DisplayName("Show Authors Display")]
        public bool ShowAuthor
        {
            get => Properties.Settings.Default.ListShowAuthor;
            set
            {
                Properties.Settings.Default.ListShowAuthor = value;
                RaisePropertyChanged(nameof(ShowAuthor));
            }
        }

        public bool ShowPublisher
        {
            get => Properties.Settings.Default.ListShowPublisher;
            set
            {
                Properties.Settings.Default.ListShowPublisher = value;
                RaisePropertyChanged(nameof(ShowPublisher));
            }
        }

        public bool ShowReleaseDate
        {
            get => Properties.Settings.Default.ListShowRelease;
            set
            {
                Properties.Settings.Default.ListShowRelease = value;
                RaisePropertyChanged(nameof(ShowReleaseDate));
            }
        }

        public bool ShowVersion
        {
            get => Properties.Settings.Default.ListShowVersion;
            set
            {
                Properties.Settings.Default.ListShowVersion = value;
                RaisePropertyChanged(nameof(ShowVersion));
            }
        }

        public bool ShowRating
        {
            get => Properties.Settings.Default.ListShowRating;
            set
            {
                Properties.Settings.Default.ListShowRating = value;
                RaisePropertyChanged(nameof(ShowRating));
            }
        }

        public bool ShowTags
        {
            get => Properties.Settings.Default.ListShowTags;
            set
            {
                Properties.Settings.Default.ListShowTags = value;
                RaisePropertyChanged(nameof(ShowTags));
            }
        }

        public bool ShowFileIcons
        {
            get => Properties.Settings.Default.ListShowFileIcons;
            set
            {
                Properties.Settings.Default.ListShowFileIcons = value;
                RaisePropertyChanged(nameof(ShowFileIcons));
            }
        }

        public bool ShowEditIcon
        {
            get => Properties.Settings.Default.ListShowEditIcon;
            set
            {
                Properties.Settings.Default.ListShowEditIcon = value;
                RaisePropertyChanged(nameof(ShowEditIcon));
            }
        }

        #endregion

    }
}
