namespace COMPASS.ViewModels
{
    internal class HomeLayoutViewModel : LayoutViewModel
    {
        public HomeLayoutViewModel() : base()
        {
            LayoutType = Layout.Home;
        }
        public double TileWidth
        {
            get => Properties.Settings.Default.HomeCoverSize;
            set
            {
                Properties.Settings.Default.HomeCoverSize = value;
                RaisePropertyChanged(nameof(TileWidth));
                RaisePropertyChanged(nameof(TileHeight));
            }
        }

        public double TileHeight => (int)(TileWidth * 4 / 3);

        public bool ShowTitle
        {
            get => Properties.Settings.Default.HomeShowTitle;
            set
            {
                Properties.Settings.Default.HomeShowTitle = value;
                RaisePropertyChanged(nameof(ShowTitle));
            }
        }
    }
}
