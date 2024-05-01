using CommunityToolkit.Mvvm.ComponentModel;

namespace COMPASS.Common.Models.Preferences
{
    public class CardLayoutPreferences : ObservableObject
    {
        private bool _showTitle = true;
        public bool ShowTitle
        {
            get => _showTitle;
            set => SetProperty(ref _showTitle, value);
        }

        private bool _showAuthor;
        public bool ShowAuthor
        {
            get => _showAuthor;
            set => SetProperty(ref _showAuthor, value);
        }

        private bool _showPublisher;
        public bool ShowPublisher
        {
            get => _showPublisher;
            set => SetProperty(ref _showPublisher, value);
        }

        private bool _showReleaseDate;
        public bool ShowReleaseDate
        {
            get => _showReleaseDate;
            set => SetProperty(ref _showReleaseDate, value);
        }

        private bool _showVersion;
        public bool ShowVersion
        {
            get => _showVersion;
            set => SetProperty(ref _showVersion, value);
        }

        private bool _showRating;
        public bool ShowRating
        {
            get => _showRating;
            set => SetProperty(ref _showRating, value);
        }

        private bool _showTags;
        public bool ShowTags
        {
            get => _showTags;
            set => SetProperty(ref _showTags, value);
        }

        private bool _showFileIcons;
        public bool ShowFileIcons
        {
            get => _showFileIcons;
            set => SetProperty(ref _showFileIcons, value);
        }
    }
}
