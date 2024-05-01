using COMPASS.Models.Preferences;
using COMPASS.Services;

namespace COMPASS.ViewModels.Layouts
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
