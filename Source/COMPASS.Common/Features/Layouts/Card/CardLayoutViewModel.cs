using COMPASS.Common.Features.CodexCollections;
using COMPASS.Common.Features.Codices;
using COMPASS.Infra.Preferences;
using COMPASS.Common.Features.Tabs;

namespace COMPASS.Common.Features.Layouts.Card
{
    public class CardLayoutViewModel : LayoutViewModel
    {
        public CardLayoutViewModel(
            CardLayoutPreferences preferences, 
            CodexInfoViewModelFactory codexInfoVmFactory,
            CodexCollectionOperations collectionOperations,
            CollectionTabVM tabVM) : base(codexInfoVmFactory, collectionOperations, tabVM)
        {
            Preferences = preferences;
        }

        public CardLayoutPreferences Preferences { get; }
        
        public override CodexLayout LayoutType => CodexLayout.Card;
    }

    public class CardLayoutViewModelFactory(
        IPreferencesService preferencesService,
        CodexInfoViewModelFactory codexInfoVmFactory,
        CodexCollectionOperations collectionOperations) : LayoutViewModelFactoryBase
    {
        public override LayoutViewModel Create(CollectionTabVM tabVm) => new CardLayoutViewModel(
            preferencesService.GetPreferences<CardLayoutPreferences>(), codexInfoVmFactory,
            collectionOperations, tabVm);
    }
}
