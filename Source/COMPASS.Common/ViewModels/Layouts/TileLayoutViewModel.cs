using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Preferences;
using COMPASS.Common.ViewModels.Main;

namespace COMPASS.Common.ViewModels.Layouts
{
    public class TileLayoutViewModel : LayoutViewModel
    {
        public TileLayoutViewModel(TileLayoutPreferences preferences, CodexInfoViewModelFactory codexInfoVmFactory, CollectionTabVM tabVM) : base(codexInfoVmFactory, tabVM)
        {
            Preferences = preferences;
        }
        
        public TileLayoutPreferences Preferences { get; }
        
        public override CodexLayout LayoutType => CodexLayout.Tile;
    }
}
