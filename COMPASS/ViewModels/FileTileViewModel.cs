using COMPASS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COMPASS.ViewModels
{
    public class FileTileViewModel : FileBaseViewModel
    {
        public FileTileViewModel(MainViewModel vm = null) : base(vm)
        {
            ViewOptions = new ObservableCollection<MyMenuItem>();
            ViewOptions.Add(new MyMenuItem("Cover Size", value => TileWidth = (double)value) { Prop = TileWidth });
            ViewOptions.Add(new MyMenuItem("Show Title", value => ShowTitle = (bool)value) { Prop = ShowTitle });
        }
        #region Properties
        private double _width = 156;
        public double TileWidth
        {
            get { return _width; }
            set 
            { 
                SetProperty(ref _width, value);
                RaisePropertyChanged(nameof(TileHeight));
            }
        }

        public double TileHeight
        {
            get { return (int)(_width * 4/3); }
        }

        private bool _showtitle = false;
        public bool ShowTitle
        {
            get { return _showtitle; }
            set { SetProperty(ref _showtitle, value); }
        }

        #endregion
    }
}
