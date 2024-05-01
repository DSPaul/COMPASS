using COMPASS.Models.Preferences;
using COMPASS.Services;

namespace COMPASS.ViewModels.Layouts
{
    public class CardLayoutViewModel : LayoutViewModel
    {
        public CardLayoutViewModel() : base()
        {
            LayoutType = Layout.Card;
            Preferences = PreferencesService.GetInstance().Preferences.CardLayoutPreferences;
        }

        public override bool DoVirtualization => Properties.Settings.Default.DoVirtualizationCard
                                                 && MainViewModel.CollectionVM.CurrentCollection.AllCodices.Count > Properties.Settings.Default.VirtualizationThresholdCard;

        public CardLayoutPreferences Preferences { get; init; }
    }
}
