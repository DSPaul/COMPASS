using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Preferences;
using COMPASS.Common.Services;

namespace COMPASS.Common.ViewModels.Layouts
{
    public class ListLayoutViewModel : LayoutViewModel
    {
        public ListLayoutViewModel() : base()
        {
            LayoutType = CodexLayout.List;
            Preferences = PreferencesService.GetInstance().Preferences.ListLayoutPreferences;
        }

        //TODO check if this is still needed
        //public override bool DoVirtualization =>
        //    Properties.Settings.Default.DoVirtualizationList &&
        //    MainViewModel.CollectionVM.CurrentCollection.AllCodices.Count > Properties.Settings.Default.VirtualizationThresholdList;

        public ListLayoutPreferences Preferences { get; }

    }
}
