namespace COMPASS.ViewModels
{
    public class TileLayoutViewModel : LayoutViewModel
    {
        public TileLayoutViewModel() : base()
        {
            LayoutType = Layout.Tile;
        }

        public enum DataOption
        {
            Title,
            Author,
            Publisher,
            Rating
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

        public bool ShowExtraData
        {
            get => Properties.Settings.Default.TileShowExtraData;
            set
            {
                Properties.Settings.Default.TileShowExtraData = value;
                RaisePropertyChanged(nameof(ShowExtraData));
            }
        }

        public DataOption DisplayedData
        {
            get => (DataOption)Properties.Settings.Default.TileDisplayedData;
            set
            {
                Properties.Settings.Default.TileDisplayedData = (int)value;
                RaisePropertyChanged(nameof(DisplayedData));
            }
        }

        #endregion
    }
}
