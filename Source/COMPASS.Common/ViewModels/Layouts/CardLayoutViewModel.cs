using COMPASS.Common.Models.Preferences;
using COMPASS.Common.Services;

namespace COMPASS.Common.ViewModels.Layouts
{
    public class CardLayoutViewModel : LayoutViewModel
    {
        public CardLayoutViewModel() : base()
        {
            LayoutType = Layout.Card;
            Preferences = PreferencesService.GetInstance().Preferences.CardLayoutPreferences;
        }

        //TODO check if this is still needed
        //public override bool DoVirtualization => Properties.Settings.Default.DoVirtualizationCard
        //                                         && MainViewModel.CollectionVM.CurrentCollection.AllCodices.Count > Properties.Settings.Default.VirtualizationThresholdCard;

        public CardLayoutPreferences Preferences { get; init; }
    }
}
