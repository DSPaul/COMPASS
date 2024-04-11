namespace COMPASS.ViewModels.Layouts
{
    public class ListLayoutViewModel : LayoutViewModel
    {
        public ListLayoutViewModel()
        {
            LayoutType = Layout.List;
        }

        public override bool DoVirtualization =>
            Properties.Settings.Default.DoVirtualizationList &&
            MainViewModel.CollectionVM.CurrentCollection.AllCodices.Count > Properties.Settings.Default.VirtualizationThresholdList;

        #region ViewOptions

        private bool _showTitle = true;
        public bool ShowTitle
        {
            get => _showTitle;
            set => SetProperty(ref _showTitle, value);
        }

        public bool ShowAuthor
        {
            get => Properties.Settings.Default.ListShowAuthor;
            set
            {
                Properties.Settings.Default.ListShowAuthor = value;
                RaisePropertyChanged();
            }
        }

        public bool ShowPublisher
        {
            get => Properties.Settings.Default.ListShowPublisher;
            set
            {
                Properties.Settings.Default.ListShowPublisher = value;
                RaisePropertyChanged();
            }
        }

        public bool ShowReleaseDate
        {
            get => Properties.Settings.Default.ListShowRelease;
            set
            {
                Properties.Settings.Default.ListShowRelease = value;
                RaisePropertyChanged();
            }
        }

        public bool ShowDateAdded
        {
            get => Properties.Settings.Default.ListShowDateAdded;
            set
            {
                Properties.Settings.Default.ListShowDateAdded = value;
                RaisePropertyChanged();
            }
        }

        public bool ShowVersion
        {
            get => Properties.Settings.Default.ListShowVersion;
            set
            {
                Properties.Settings.Default.ListShowVersion = value;
                RaisePropertyChanged();
            }
        }

        public bool ShowRating
        {
            get => Properties.Settings.Default.ListShowRating;
            set
            {
                Properties.Settings.Default.ListShowRating = value;
                RaisePropertyChanged();
            }
        }

        public bool ShowISBN
        {
            get => Properties.Settings.Default.ListShowISBN;
            set
            {
                Properties.Settings.Default.ListShowISBN = value;
                RaisePropertyChanged();
            }
        }

        public bool ShowTags
        {
            get => Properties.Settings.Default.ListShowTags;
            set
            {
                Properties.Settings.Default.ListShowTags = value;
                RaisePropertyChanged();
            }
        }

        public bool ShowFileIcons
        {
            get => Properties.Settings.Default.ListShowFileIcons;
            set
            {
                Properties.Settings.Default.ListShowFileIcons = value;
                RaisePropertyChanged();
            }
        }

        public bool ShowEditIcon
        {
            get => Properties.Settings.Default.ListShowEditIcon;
            set
            {
                Properties.Settings.Default.ListShowEditIcon = value;
                RaisePropertyChanged();
            }
        }

        #endregion

    }
}
