using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Preferences;
using COMPASS.Common.Services;
using Material.Icons;

namespace COMPASS.Common.ViewModels.Layouts
{
    public class TileLayoutViewModel : LayoutViewModel
    {
        public TileLayoutViewModel() : base()
        {
            Preferences = PreferencesService.GetInstance().Preferences.TileLayoutPreferences;
        }

        //TODO check if this is still needed
        //public override bool DoVirtualization => Properties.Settings.Default.DoVirtualizationTile
        //                                         && MainViewModel.CollectionVM.CurrentCollection.AllCodices.Count > Properties.Settings.Default.VirtualizationThresholdTile;

        public TileLayoutPreferences Preferences { get; }
        public override CodexLayout LayoutType => CodexLayout.Tile;
        public override string Name  => "Tile";
        public override MaterialIconKind Icon  => MaterialIconKind.ViewGrid;
    }
}
