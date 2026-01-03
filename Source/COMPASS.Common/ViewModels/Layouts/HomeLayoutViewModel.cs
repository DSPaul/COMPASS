using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Preferences;
using COMPASS.Common.Services;
using COMPASS.Common.ViewModels.Main;

namespace COMPASS.Common.ViewModels.Layouts
{
    internal class HomeLayoutViewModel : LayoutViewModel
    {
        public HomeLayoutViewModel(CollectionTabVM tabVM) : base(tabVM)
        {
            Preferences = PreferencesService.GetInstance().Preferences.HomeLayoutPreferences;
        }

        //public override bool DoVirtualization => false;

        public HomeLayoutPreferences Preferences { get; set; }
        
        public override CodexLayout LayoutType => CodexLayout.Home;
    }
}
