using COMPASS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COMPASS.ViewModels
{
    public class FileCardViewModel : FileBaseViewModel
    {
        public FileCardViewModel(MainViewModel vm = null) : base(vm)
        {
            //MyMenuItem Datavisibility = new MyMenuItem("Show Metadata")
            //{
            //    Submenus = new ObservableCollection<MyMenuItem>()
            //};

            //Datavisibility.Submenus.Add(new MyMenuItem("Title", value => ShowTitle = (bool)value) { Prop = ShowTitle });
            ViewOptions.Add(new MyMenuItem("Publisher", value => ShowPublisher = (bool)value) { Prop = ShowPublisher });
            ViewOptions.Add(new MyMenuItem("Release Date", value => ShowReleaseDate = (bool)value) { Prop = ShowReleaseDate });
            ViewOptions.Add(new MyMenuItem("Version", value => ShowVersion = (bool)value) { Prop = ShowVersion });
            ViewOptions.Add(new MyMenuItem("Rating", value => ShowRating = (bool)value) { Prop = ShowRating });
            ViewOptions.Add(new MyMenuItem("Author", value => ShowAuthor = (bool)value) { Prop = ShowAuthor });
            ViewOptions.Add(new MyMenuItem("Tags", value => ShowTags = (bool)value) { Prop = ShowTags });
            ViewOptions.Add(new MyMenuItem("File Icons", value => ShowFileIcons = (bool)value) { Prop = ShowFileIcons });
            //Datavisibility.Submenus.Add(new MyMenuItem("Edit Icon", value => ShowEditIcon = (bool)value) { Prop = ShowEditIcon });

            ViewOptions.Add(SortOptionsMenuItem);

            //ViewOptions.Add(Datavisibility);
        }

        #region ViewOptions

        private bool _showTitle = true;
        public bool ShowTitle
        {
            get { return _showTitle; }
            set { SetProperty(ref _showTitle, value); }
        }

        private bool _showAuthor = Properties.Settings.Default.CardShowAuthor;
        public bool ShowAuthor
        {
            get { return _showAuthor; }
            set
            {
                SetProperty(ref _showAuthor, value);
                Properties.Settings.Default.CardShowAuthor = value;
            }
        }

        private bool _showPublisher = Properties.Settings.Default.CardShowPublisher;
        public bool ShowPublisher
        {
            get { return _showPublisher; }
            set
            {
                SetProperty(ref _showPublisher, value);
                Properties.Settings.Default.CardShowPublisher = value;
            }
        }

        private bool _ShowReleaseDate = Properties.Settings.Default.CardShowRelease;
        public bool ShowReleaseDate
        {
            get { return _ShowReleaseDate; }
            set
            {
                SetProperty(ref _ShowReleaseDate, value);
                Properties.Settings.Default.CardShowRelease = value;
            }
        }

        private bool _showVersion = Properties.Settings.Default.CardShowVersion;
        public bool ShowVersion
        {
            get { return _showVersion; }
            set
            {
                SetProperty(ref _showVersion, value);
                Properties.Settings.Default.CardShowVersion = value;
            }
        }

        private bool _showRating = Properties.Settings.Default.CardShowRating;
        public bool ShowRating
        {
            get { return _showRating; }
            set
            {
                SetProperty(ref _showRating, value);
                Properties.Settings.Default.CardShowRating = value;
            }
        }

        private bool _showTags = Properties.Settings.Default.CardShowTags;
        public bool ShowTags
        {
            get { return _showTags; }
            set
            {
                SetProperty(ref _showTags, value);
                Properties.Settings.Default.CardShowTags = value;
            }
        }

        private bool _showFileIcons = Properties.Settings.Default.CardShowFileIcons;
        public bool ShowFileIcons
        {
            get { return _showFileIcons; }
            set
            {
                SetProperty(ref _showFileIcons, value);
                Properties.Settings.Default.CardShowFileIcons = value;
            }
        }

        #endregion

    }
}
