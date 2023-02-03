using COMPASS.Tools;

namespace COMPASS.ViewModels
{
    public class TileLayoutViewModel : LayoutViewModel
    {
        public TileLayoutViewModel() : base()
        {
            LayoutType = Enums.CodexLayout.TileLayout;
        }
        #region Properties
        public double TileWidth
        {
            get => Properties.Settings.Default.TileCoverSize;
            set
            {
                Properties.Settings.Default.TileCoverSize = value;
                RaisePropertyChanged(nameof(TileWidth));
                RaisePropertyChanged(nameof(TileHeight));
            }
        }

        public double TileHeight => (int)(TileWidth * 4 / 3);

        public bool ShowTitle
        {
            get { return Properties.Settings.Default.TileShowTitle; ; }
            set
            {
                Properties.Settings.Default.TileShowTitle = value;
                RaisePropertyChanged(nameof(ShowTitle));
            }
        }

        #endregion
    }
}
