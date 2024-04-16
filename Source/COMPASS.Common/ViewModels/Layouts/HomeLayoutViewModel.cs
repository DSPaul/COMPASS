namespace COMPASS.Common.ViewModels.Layouts
{
    internal class HomeLayoutViewModel : LayoutViewModel
    {
        public HomeLayoutViewModel() : base()
        {
            LayoutType = Layout.Home;
        }

        public override bool DoVirtualization => false;

        public double TileWidth
        {
            get => Properties.Settings.Default.HomeCoverSize;
            set
            {
                Properties.Settings.Default.HomeCoverSize = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TileHeight));
            }
        }

        public double TileHeight => (int)(TileWidth * 4 / 3);

        public bool ShowTitle
        {
            get => Properties.Settings.Default.HomeShowTitle;
            set
            {
                Properties.Settings.Default.HomeShowTitle = value;
                OnPropertyChanged();
            }
        }
    }
}
