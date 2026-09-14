using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Preferences;
using COMPASS.Common.Operations;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.ModelVMs;

namespace COMPASS.Common.ViewModels.Layouts
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
            preferencesService.Preferences.CardLayoutPreferences, codexInfoVmFactory,
            importFilesVmFactory, collectionOperations, tabVm);
    }
}
