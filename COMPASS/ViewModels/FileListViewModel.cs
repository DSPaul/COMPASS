using COMPASS.Models;
using COMPASS.ViewModels.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COMPASS.ViewModels
{
    public class FileListViewModel : FileBaseViewModel
    {
        public FileListViewModel( MainViewModel vm) : base(vm)
        {
            MyMenuItem Columnvisibility = new MyMenuItem("Column Visibility")
            {
                Submenus = new ObservableCollection<MyMenuItem>()
            };

            //Columnvisibility.Submenus.Add(new MyMenuItem("Title", value => ShowTitle = (bool)value) { Prop = ShowTitle });
            Columnvisibility.Submenus.Add(new MyMenuItem("Author", value => ShowAuthor = (bool)value) { Prop = ShowAuthor});
            Columnvisibility.Submenus.Add(new MyMenuItem("Publisher", value => ShowPublisher= (bool)value) { Prop = ShowPublisher });
            Columnvisibility.Submenus.Add(new MyMenuItem("Release Date", value => ShowReleaseDate = (bool)value) { Prop = ShowReleaseDate });
            Columnvisibility.Submenus.Add(new MyMenuItem("Version", value => ShowVersion = (bool)value) { Prop = ShowVersion });
            Columnvisibility.Submenus.Add(new MyMenuItem("Rating", value => ShowRating = (bool)value) { Prop = ShowRating });
            Columnvisibility.Submenus.Add(new MyMenuItem("Tags", value => ShowTags = (bool)value) { Prop = ShowTags });
            Columnvisibility.Submenus.Add(new MyMenuItem("File Icons", value => ShowFileIcons = (bool)value) { Prop = ShowFileIcons });
            //Columnvisibility.Submenus.Add(new MyMenuItem("Edit Icon", value => ShowEditIcon = (bool)value) { Prop = ShowEditIcon });
            
            ViewOptions.Add(Columnvisibility);
        }

        #region ViewOptions

        private bool _showTitle = true;
        public bool ShowTitle 
        {
            get {return _showTitle; }
            set{ SetProperty(ref _showTitle, value); }
        }

        private bool _showAuthor = Properties.Settings.Default.ListShowAuthor;
        public bool ShowAuthor
        {
            get { return _showAuthor; }
            set { SetProperty(ref _showAuthor, value);
                Properties.Settings.Default.ListShowAuthor = value;
            }
        }

        private bool _showPublisher = Properties.Settings.Default.ListShowPublisher;
        public bool ShowPublisher
        {
            get { return _showPublisher; }
            set { SetProperty(ref _showPublisher, value);
                Properties.Settings.Default.ListShowPublisher = value;
            }
        }

        private bool _ShowReleaseDate = Properties.Settings.Default.ListShowRelease;
        public bool ShowReleaseDate
        {
            get { return _ShowReleaseDate; }
            set { SetProperty(ref _ShowReleaseDate, value);
                Properties.Settings.Default.ListShowRelease = value;
            }
        }

        private bool _showVersion = Properties.Settings.Default.ListShowVersion;
        public bool ShowVersion
        {
            get { return _showVersion; }
            set { SetProperty(ref _showVersion, value);
                Properties.Settings.Default.ListShowVersion = value;
            }
        }

        private bool _showRating = Properties.Settings.Default.ListShowRating;
        public bool ShowRating
        {
            get { return _showRating; }
            set { SetProperty(ref _showRating, value);
                Properties.Settings.Default.ListShowRating = value;
            }
        }

        private bool _showTags = Properties.Settings.Default.ListShowTags;
        public bool ShowTags
        {
            get { return _showTags; }
            set { SetProperty(ref _showTags, value);
                Properties.Settings.Default.ListShowTags = value;
            }
        }

        private bool _showFileIcons = Properties.Settings.Default.ListShowFileIcons;
        public bool ShowFileIcons
        {
            get { return _showFileIcons; }
            set { SetProperty(ref _showFileIcons, value);
                Properties.Settings.Default.ListShowFileIcons = value;
            }
        }

        private bool _showEditIcon = Properties.Settings.Default.ListShowEditIcon;
        public bool ShowEditIcon
        {
            get { return _showEditIcon; }
            set { SetProperty(ref _showEditIcon, value);
                Properties.Settings.Default.ListShowEditIcon = value;
            }
        }

        #endregion

    }
}
