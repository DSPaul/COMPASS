using COMPASS.Common.Features.CodexCollections;
using COMPASS.Common.Features.Codices;
using COMPASS.Common.Features.ImportExport;
using COMPASS.Infra.Preferences;
using COMPASS.Common.Features.Tabs;

namespace COMPASS.Common.Features.Layouts.Card
{
    public class CardLayoutViewModel : LayoutViewModel
    {
        public CardLayoutViewModel(
            CardLayoutPreferences preferences, 
            CodexInfoViewModelFactory codexInfoVmFactory,
            ImportFilesViewModelFactory importFilesVmFactory,
            CodexCollectionOperations collectionOperations,
            CollectionTabVM tabVM) : base(codexInfoVmFactory, importFilesVmFactory, collectionOperations, tabVM)
        {
            Preferences = preferences;
        }

        public CardLayoutPreferences Preferences { get; }
        
        public override CodexLayout LayoutType => CodexLayout.Card;
    }

    public class CardLayoutViewModelFactory(
        IPreferencesService preferencesService,
        CodexInfoViewModelFactory codexInfoVmFactory,
        ImportFilesViewModelFactory importFilesVmFactory,
        CodexCollectionOperations collectionOperations) : LayoutViewModelFactoryBase
    {
        public override LayoutViewModel Create(CollectionTabVM tabVm) => new CardLayoutViewModel(
            preferencesService.GetPreferences<CardLayoutPreferences>(), codexInfoVmFactory,
            importFilesVmFactory, collectionOperations, tabVm);
    }
}
