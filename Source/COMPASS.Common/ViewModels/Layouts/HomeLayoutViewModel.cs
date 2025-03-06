using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Preferences;
using COMPASS.Common.Services;
using Material.Icons;

namespace COMPASS.Common.ViewModels.Layouts
{
    internal class HomeLayoutViewModel : LayoutViewModel
    {
        public HomeLayoutViewModel() : base()
        {
            Preferences = PreferencesService.GetInstance().Preferences.HomeLayoutPreferences;
        }

        //public override bool DoVirtualization => false;

        public HomeLayoutPreferences Preferences { get; set; }
        public override CodexLayout LayoutType => CodexLayout.Home;
        public override string Name  => "Home";
        public override MaterialIconKind Icon  => MaterialIconKind.Home;
    }
}
