using COMPASS.Models;
using COMPASS.ViewModels.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            Columnvisibility.Submenus.Add(new MyMenuItem("Title") { Prop = (bool)ShowTitle });
            Columnvisibility.Submenus.Add(new MyMenuItem("Author") { Prop = (bool)ShowAuthor });
            Columnvisibility.Submenus.Add(new MyMenuItem("Publisher") { Prop = (bool)ShowPublisher });
            Columnvisibility.Submenus.Add(new MyMenuItem("Tags") { Prop = (bool)ShowTags });
            Columnvisibility.Submenus.Add(new MyMenuItem("File Icons") { Prop = (bool)ShowFileIcons });
            Columnvisibility.Submenus.Add(new MyMenuItem("Edit Icon") { Prop = (bool)ShowEditIcon });
            ViewOptions.Add(Columnvisibility);
        }

        #region ViewOptions

        private bool _showTitle;
        public bool ShowTitle 
        {
            get {return _showTitle; }
            set{ SetProperty(ref _showTitle, value); }
        }

        private bool _showAuthor;
        public bool ShowAuthor
        {
            get { return _showAuthor; }
            set { SetProperty(ref _showAuthor, value); }
        }

        private bool _showPublisher;
        public bool ShowPublisher
        {
            get { return _showPublisher; }
            set { SetProperty(ref _showPublisher, value); }
        }

        private bool _showTags;
        public bool ShowTags
        {
            get { return _showTags; }
            set { SetProperty(ref _showTags, value); }
        }

        private bool _showFileIcons;
        public bool ShowFileIcons
        {
            get { return _showFileIcons; }
            set { SetProperty(ref _showFileIcons, value); }
        }

        private bool _showEditIcon;
        public bool ShowEditIcon
        {
            get { return _showEditIcon; }
            set { SetProperty(ref _showEditIcon, value); }
        }

        #endregion

    }
}
