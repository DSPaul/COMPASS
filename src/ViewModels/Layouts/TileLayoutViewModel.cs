using COMPASS.Models;
using COMPASS.Tools;

namespace COMPASS.ViewModels
{
    public class TileLayoutViewModel : LayoutViewModel
    {
        public TileLayoutViewModel() : base()
        {
            LayoutType = Enums.CodexLayout.TileLayout;

            ViewOptions.Add(new MyMenuItem("Cover Size", value => TileWidth = (double)value) { Prop = TileWidth });
            ViewOptions.Add(new MyMenuItem("Show Title", value => ShowTitle = (bool)value) { Prop = ShowTitle });
            ViewOptions.Add(SortOptionsMenuItem);
        }
        #region Properties
        private double _width = Properties.Settings.Default.TileCoverSize;
        public double TileWidth
        {
            get { return _width; }
            set
            {
                SetProperty(ref _width, value);
                RaisePropertyChanged(nameof(TileHeight));
                Properties.Settings.Default.TileCoverSize = value;
            }
        }

        public double TileHeight
        {
            get { return (int)(_width * 4 / 3); }
        }

        private bool _showtitle = Properties.Settings.Default.TileShowTitle;
        public bool ShowTitle
        {
            get { return _showtitle; }
            set
            {
                SetProperty(ref _showtitle, value);
                Properties.Settings.Default.TileShowTitle = value;
            }
        }

        #endregion
    }
}
