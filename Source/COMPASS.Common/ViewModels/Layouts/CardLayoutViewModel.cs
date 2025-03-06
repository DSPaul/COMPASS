using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Preferences;
using COMPASS.Common.Services;
using Material.Icons;

namespace COMPASS.Common.ViewModels.Layouts
{
    public class CardLayoutViewModel : LayoutViewModel
    {
        public CardLayoutViewModel() : base()
        {
            Preferences = PreferencesService.GetInstance().Preferences.CardLayoutPreferences;
        }

        //TODO check if this is still needed
        //public override bool DoVirtualization => Properties.Settings.Default.DoVirtualizationCard
        //                                         && MainViewModel.CollectionVM.CurrentCollection.AllCodices.Count > Properties.Settings.Default.VirtualizationThresholdCard;

        public CardLayoutPreferences Preferences { get; }
        
        public override CodexLayout LayoutType => CodexLayout.Card;
        public override string Name  => "Card";
        public override MaterialIconKind Icon  => MaterialIconKind.FormatListText;
    }
}
