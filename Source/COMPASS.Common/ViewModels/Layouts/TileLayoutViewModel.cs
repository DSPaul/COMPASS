namespace COMPASS.Common.ViewModels.Layouts
{
    public class TileLayoutViewModel : LayoutViewModel
    {
        public TileLayoutViewModel()
        {
            LayoutType = Layout.Tile;
        }

        public override bool DoVirtualization => Properties.Settings.Default.DoVirtualizationTile 
                                                 && MainViewModel.CollectionVM.CurrentCollection.AllCodices.Count > Properties.Settings.Default.VirtualizationThresholdTile;

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
                OnPropertyChanged();
                OnPropertyChanged(nameof(TileHeight));
            }
        }

        public double TileHeight => (int)(TileWidth * 4 / 3);

        public bool ShowExtraData
        {
            get => Properties.Settings.Default.TileShowExtraData;
            set
            {
                Properties.Settings.Default.TileShowExtraData = value;
                OnPropertyChanged();
            }
        }

        public DataOption DisplayedData
        {
            get => (DataOption)Properties.Settings.Default.TileDisplayedData;
            set
            {
                Properties.Settings.Default.TileDisplayedData = (int)value;
                OnPropertyChanged();
            }
        }

        #endregion
    }
}
