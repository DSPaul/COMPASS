using COMPASS.Common.Features.CodexCollections;
using COMPASS.Common.Features.Codices;
using COMPASS.Infra.Preferences;
using COMPASS.Common.Features.Tabs;

namespace COMPASS.Common.Features.Layouts.Tile
{
    public class TileLayoutViewModel : LayoutViewModel
    {
        public TileLayoutViewModel(
            TileLayoutPreferences preferences, 
            CodexInfoViewModelFactory codexInfoVmFactory,
            CodexCollectionOperations collectionOperations,
            CollectionTabVM tabVM) : base(codexInfoVmFactory, collectionOperations, tabVM)
        {
            Preferences = preferences;
        }
        
        public TileLayoutPreferences Preferences { get; }
        
        public override CodexLayout LayoutType => CodexLayout.Tile;
    }

    public class TileLayoutViewModelFactory(
        IPreferencesService preferencesService,
        CodexInfoViewModelFactory codexInfoVmFactory,
        CodexCollectionOperations collectionOperations) : LayoutViewModelFactoryBase
    {
        public override LayoutViewModel Create(CollectionTabVM tabVm) => new TileLayoutViewModel(
            preferencesService.GetPreferences<TileLayoutPreferences>(), codexInfoVmFactory,
            collectionOperations, tabVm);
    }
}
