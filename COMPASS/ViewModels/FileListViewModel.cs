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
            ViewOptions = new ObservableCollection<MyMenuItem>();

            MyMenuItem Columnvisibility = new MyMenuItem("Column Visibility")
            {
                Submenus = new ObservableCollection<MyMenuItem>()
            };

            Columnvisibility.Submenus.Add(new MyMenuItem("Title", value => ShowTitle = (bool)value) { Prop = ShowTitle });
            Columnvisibility.Submenus.Add(new MyMenuItem("Author", value => ShowAuthor = (bool)value) { Prop = ShowAuthor});
            Columnvisibility.Submenus.Add(new MyMenuItem("Publisher", value => ShowPublisher= (bool)value) { Prop = ShowPublisher });
            Columnvisibility.Submenus.Add(new MyMenuItem("Release Date", value => ShowReleaseDate = (bool)value) { Prop = ShowReleaseDate });
            Columnvisibility.Submenus.Add(new MyMenuItem("Version", value => ShowVersion = (bool)value) { Prop = ShowVersion });
            Columnvisibility.Submenus.Add(new MyMenuItem("Tags", value => ShowTags = (bool)value) { Prop = ShowTags });
            Columnvisibility.Submenus.Add(new MyMenuItem("File Icons", value => ShowFileIcons = (bool)value) { Prop = ShowFileIcons });
            Columnvisibility.Submenus.Add(new MyMenuItem("Edit Icon", value => ShowEditIcon = (bool)value) { Prop = ShowEditIcon });
            
            ViewOptions.Add(Columnvisibility);
        }

        #region ViewOptions

        private bool _showTitle = true;
        public bool ShowTitle 
        {
            get {return _showTitle; }
            set{ SetProperty(ref _showTitle, value); }
        }

        private bool _showAuthor = true;
        public bool ShowAuthor
        {
            get { return _showAuthor; }
            set { SetProperty(ref _showAuthor, value); }
        }

        private bool _showPublisher = false;
        public bool ShowPublisher
        {
            get { return _showPublisher; }
            set { SetProperty(ref _showPublisher, value); }
        }

        private bool _ShowReleaseDate = true;
        public bool ShowReleaseDate
        {
            get { return _ShowReleaseDate; }
            set { SetProperty(ref _ShowReleaseDate, value); }
        }

        private bool _showVersion = true;
        public bool ShowVersion
        {
            get { return _showVersion; }
            set { SetProperty(ref _showVersion, value); }
        }

        private bool _showTags = true;
        public bool ShowTags
        {
            get { return _showTags; }
            set { SetProperty(ref _showTags, value); }
        }

        private bool _showFileIcons = true;
        public bool ShowFileIcons
        {
            get { return _showFileIcons; }
            set { SetProperty(ref _showFileIcons, value); }
        }

        private bool _showEditIcon = true;
        public bool ShowEditIcon
        {
            get { return _showEditIcon; }
            set { SetProperty(ref _showEditIcon, value); }
        }

        #endregion

    }
}
