using CommunityToolkit.Mvvm.ComponentModel;

namespace COMPASS.Models.Preferences
{
    public class HomeLayoutPreferences : ObservableObject
    {
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
