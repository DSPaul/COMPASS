using COMPASS.Common.Models.Preferences;
using COMPASS.Common.Services;

namespace COMPASS.Common.ViewModels.Layouts
{
    internal class HomeLayoutViewModel : LayoutViewModel
    {
        public HomeLayoutViewModel() : base()
        {
            LayoutType = Layout.Home;
            Preferences = PreferencesService.GetInstance().Preferences.HomeLayoutPreferences;
        }

        public override bool DoVirtualization => false;

        public HomeLayoutPreferences Preferences { get; set; }
    }
}
