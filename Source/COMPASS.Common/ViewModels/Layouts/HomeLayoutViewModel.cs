using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Preferences;
using COMPASS.Common.ViewModels.Main;

namespace COMPASS.Common.ViewModels.Layouts
{
    internal class HomeLayoutViewModel : LayoutViewModel
    {
        public HomeLayoutViewModel(CollectionTabVM tabVM) : base(tabVM)
        {
            Preferences = PreferencesService.Preferences.HomeLayoutPreferences;
        }

        public HomeLayoutPreferences Preferences { get; set; }
        
        public override CodexLayout LayoutType => CodexLayout.Home;
    }
}
