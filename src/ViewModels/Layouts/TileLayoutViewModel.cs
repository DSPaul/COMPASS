using COMPASS.Models.Preferences;
using COMPASS.Services;

namespace COMPASS.ViewModels.Layouts
{
    public class TileLayoutViewModel : LayoutViewModel
    {
        public TileLayoutViewModel()
        {
            LayoutType = Layout.Tile;
            Preferences = PreferencesService.GetInstance().Preferences.TileLayoutPreferences;
        }

        public override bool DoVirtualization => Properties.Settings.Default.DoVirtualizationTile
                                                 && MainViewModel.CollectionVM.CurrentCollection.AllCodices.Count > Properties.Settings.Default.VirtualizationThresholdTile;

        public TileLayoutPreferences Preferences { get; set; }
    }
}
