namespace COMPASS.Common.ViewModels.Layouts
{
    public class CardLayoutViewModel : LayoutViewModel
    {
        public CardLayoutViewModel() : base()
        {
            LayoutType = Layout.Card;
        }

        public override bool DoVirtualization => Properties.Settings.Default.DoVirtualizationCard 
                                                 && MainViewModel.CollectionVM.CurrentCollection.AllCodices.Count > Properties.Settings.Default.VirtualizationThresholdCard;

        #region Options

        private bool _showTitle = true;
        public bool ShowTitle
        {
            get => _showTitle;
            set => SetProperty(ref _showTitle, value);
        }

        public bool ShowAuthor
        {
            get => Properties.Settings.Default.CardShowAuthor;
            set
            {
                Properties.Settings.Default.CardShowAuthor = value;
                RaisePropertyChanged();
            }
        }

        public bool ShowPublisher
        {
            get => Properties.Settings.Default.CardShowPublisher;
            set
            {
                Properties.Settings.Default.CardShowPublisher = value;
                RaisePropertyChanged();
            }
        }

        public bool ShowReleaseDate
        {
            get => Properties.Settings.Default.CardShowRelease;
            set
            {
                Properties.Settings.Default.CardShowRelease = value;
                RaisePropertyChanged();
            }
        }

        public bool ShowVersion
        {
            get => Properties.Settings.Default.CardShowVersion;
            set
            {
                Properties.Settings.Default.CardShowVersion = value;
                RaisePropertyChanged();
            }
        }

        public bool ShowRating
        {
            get => Properties.Settings.Default.CardShowRating;
            set
            {
                Properties.Settings.Default.CardShowRating = value;
                RaisePropertyChanged();
            }
        }

        public bool ShowTags
        {
            get => Properties.Settings.Default.CardShowTags;
            set
            {
                Properties.Settings.Default.CardShowTags = value;
                RaisePropertyChanged();
            }
        }

        public bool ShowFileIcons
        {
            get => Properties.Settings.Default.CardShowFileIcons;
            set
            {
                Properties.Settings.Default.CardShowFileIcons = value;
                RaisePropertyChanged();
            }
        }

        #endregion

    }
}
