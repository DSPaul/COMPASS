using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Preferences;
using COMPASS.Common.ViewModels.Main;

namespace COMPASS.Common.ViewModels.Layouts
{
    public class CardLayoutViewModel : LayoutViewModel
    {
        public CardLayoutViewModel(CardLayoutPreferences preferences, CodexInfoViewModelFactory codexInfoVmFactory, CollectionTabVM tabVM) : base(codexInfoVmFactory, tabVM)
        {
            Preferences = preferences;
        }

        public CardLayoutPreferences Preferences { get; }
        
        public override CodexLayout LayoutType => CodexLayout.Card;
    }
}
