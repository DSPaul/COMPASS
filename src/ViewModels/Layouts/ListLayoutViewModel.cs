using COMPASS.Models;
using COMPASS.Tools;
using System.ComponentModel;

namespace COMPASS.ViewModels
{
    public class ListLayoutViewModel : LayoutViewModel
    {
        public ListLayoutViewModel() : base()
        {
            LayoutType = Enums.CodexLayout.ListLayout;

            ViewOptions.Add(new MyMenuItem("Author", () => ShowAuthor, value => ShowAuthor = (bool)value));
            ViewOptions.Add(new MyMenuItem("Publisher", () => ShowPublisher, value => ShowPublisher = (bool)value));
            ViewOptions.Add(new MyMenuItem("Release Date", () => ShowReleaseDate, value => ShowReleaseDate = (bool)value));
            ViewOptions.Add(new MyMenuItem("Version", () => ShowVersion, value => ShowVersion = (bool)value));
            ViewOptions.Add(new MyMenuItem("Rating", () => ShowRating, value => ShowRating = (bool)value));
            ViewOptions.Add(new MyMenuItem("Tags", () => ShowTags, value => ShowTags = (bool)value));
            ViewOptions.Add(new MyMenuItem("File Icons", () => ShowFileIcons, value => ShowFileIcons = (bool)value));

        }

        #region ViewOptions

        private bool _showTitle = true;
        public bool ShowTitle
        {
            get { return _showTitle; }
            set { SetProperty(ref _showTitle, value); }
        }

        [DisplayName("Show Authors Display")]
        public bool ShowAuthor
        {
            get { return Properties.Settings.Default.ListShowAuthor; }
            set
            {
                Properties.Settings.Default.ListShowAuthor = value;
                RaisePropertyChanged(nameof(ShowAuthor));
            }
        }

        public bool ShowPublisher
        {
            get { return Properties.Settings.Default.ListShowPublisher; }
            set
            {
                Properties.Settings.Default.ListShowPublisher = value;
                RaisePropertyChanged(nameof(ShowPublisher));
            }
        }

        public bool ShowReleaseDate
        {
            get { return Properties.Settings.Default.ListShowRelease; }
            set
            {
                Properties.Settings.Default.ListShowRelease = value;
                RaisePropertyChanged(nameof(ShowReleaseDate));
            }
        }

        public bool ShowVersion
        {
            get { return Properties.Settings.Default.ListShowVersion; }
            set
            {
                Properties.Settings.Default.ListShowVersion = value;
                RaisePropertyChanged(nameof(ShowVersion));
            }
        }

        public bool ShowRating
        {
            get { return Properties.Settings.Default.ListShowRating; }
            set
            {
                Properties.Settings.Default.ListShowRating = value;
                RaisePropertyChanged(nameof(ShowRating));
            }
        }

        public bool ShowTags
        {
            get { return Properties.Settings.Default.ListShowTags; }
            set
            {
                Properties.Settings.Default.ListShowTags = value;
                RaisePropertyChanged(nameof(ShowTags));
            }
        }

        public bool ShowFileIcons
        {
            get { return Properties.Settings.Default.ListShowFileIcons; }
            set
            {
                Properties.Settings.Default.ListShowFileIcons = value;
                RaisePropertyChanged(nameof(ShowFileIcons));
            }
        }

        public bool ShowEditIcon
        {
            get { return Properties.Settings.Default.ListShowEditIcon; }
            set
            {
                Properties.Settings.Default.ListShowEditIcon = value;
                RaisePropertyChanged(nameof(ShowEditIcon));
            }
        }

        #endregion

    }
}
