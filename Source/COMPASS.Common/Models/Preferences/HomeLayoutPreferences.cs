using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;

namespace COMPASS.Common.Models.Preferences
{
    public class HomeLayoutPreferences : ObservableObject
    {
        private double _tileWidth = 100;
        public double TileWidth
        {
            get => _tileWidth;
            set
            {
                SetProperty(ref _tileWidth, value);
                OnPropertyChanged(nameof(TileHeight));
                OnPropertyChanged(nameof(TilePanelMargin));
            }
        }

        public double TileHeight => (int)(TileWidth * 4 / 3);

        public Thickness TilePanelMargin
        {
            get
            {
                double sideMargin = Math.Clamp(TileWidth * 0.15, 5, 25);
                return new(sideMargin, 15, sideMargin, 5);
            }
        }

        private bool _showTitle;
        public bool ShowTitle
        {
            get => _showTitle;
            set => SetProperty(ref _showTitle, value);
        }
    }
}
