using COMPASS.Tools;

namespace COMPASS.ViewModels
{
    public class CardLayoutViewModel : LayoutViewModel
    {
        public CardLayoutViewModel() : base()
        {
            LayoutType = Enums.CodexLayout.CardLayout;
        }

        #region VOptions

        private bool _showTitle = true;
        public bool ShowTitle
        {
            get { return _showTitle; }
            set { SetProperty(ref _showTitle, value); }
        }

        public bool ShowAuthor
        {
            get { return Properties.Settings.Default.CardShowAuthor; }
            set
            {
                Properties.Settings.Default.CardShowAuthor = value;
                RaisePropertyChanged(nameof(ShowAuthor));
            }
        }

        public bool ShowPublisher
        {
            get { return Properties.Settings.Default.CardShowPublisher; }
            set
            {
                Properties.Settings.Default.CardShowPublisher = value;
                RaisePropertyChanged(nameof(ShowPublisher));
            }
        }

        public bool ShowReleaseDate
        {
            get { return Properties.Settings.Default.CardShowRelease; }
            set
            {
                Properties.Settings.Default.CardShowRelease = value;
                RaisePropertyChanged(nameof(ShowReleaseDate));
            }
        }

        public bool ShowVersion
        {
            get { return Properties.Settings.Default.CardShowVersion; }
            set
            {
                Properties.Settings.Default.CardShowVersion = value;
                RaisePropertyChanged(nameof(ShowVersion));
            }
        }

        public bool ShowRating
        {
            get { return Properties.Settings.Default.CardShowRating; }
            set
            {
                Properties.Settings.Default.CardShowRating = value;
                RaisePropertyChanged(nameof(ShowRating));
            }
        }

        public bool ShowTags
        {
            get { return Properties.Settings.Default.CardShowTags; }
            set
            {
                Properties.Settings.Default.CardShowTags = value;
                RaisePropertyChanged(nameof(ShowTags));
            }
        }

        public bool ShowFileIcons
        {
            get { return Properties.Settings.Default.CardShowFileIcons; }
            set
            {
                Properties.Settings.Default.CardShowFileIcons = value;
                RaisePropertyChanged(nameof(ShowFileIcons));
            }
        }

        #endregion

    }
}
