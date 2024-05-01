using CommunityToolkit.Mvvm.ComponentModel;

namespace COMPASS.Common.Models.Preferences
{
    public class HomeLayoutPreferences : ObservableObject
    {
        private double _tileWidth;
        public double TileWidth
        {
            get => _tileWidth;
            set
            {
                SetProperty(ref _tileWidth, value);
                OnPropertyChanged(nameof(TileHeight));
            }
        }

        public double TileHeight => (int)(TileWidth * 4 / 3);

        private bool _showTitle;
        public bool ShowTitle
        {
            get => _showTitle;
            set => SetProperty(ref _showTitle, value);
        }
    }
}
