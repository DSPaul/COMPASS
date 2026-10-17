using COMPASS.Common.Features.CodexCollections;
using COMPASS.Common.Features.Codices;
using COMPASS.Common.Features.ImportExport;
using COMPASS.Infra.Preferences;
using COMPASS.Common.Features.Tabs;

namespace COMPASS.Common.Features.Layouts.Tile
{
    public class TileLayoutViewModel : LayoutViewModel
    {
        public TileLayoutViewModel(
            TileLayoutPreferences preferences, 
            CodexInfoViewModelFactory codexInfoVmFactory,
            ImportFilesViewModelFactory importFilesVmFactory,
            CodexCollectionOperations collectionOperations,
            CollectionTabVM tabVM) : base(codexInfoVmFactory, importFilesVmFactory, collectionOperations, tabVM)
        {
            Preferences = preferences;
        }
        
        public TileLayoutPreferences Preferences { get; }
        
        public override CodexLayout LayoutType => CodexLayout.Tile;
    }

    public class TileLayoutViewModelFactory(
        IPreferencesService preferencesService,
        CodexInfoViewModelFactory codexInfoVmFactory,
        ImportFilesViewModelFactory importFilesVmFactory,
        CodexCollectionOperations collectionOperations) : LayoutViewModelFactoryBase
    {
        public override LayoutViewModel Create(CollectionTabVM tabVm) => new TileLayoutViewModel(
            preferencesService.GetPreferences<TileLayoutPreferences>(), codexInfoVmFactory,
            importFilesVmFactory, collectionOperations, tabVm);
    }
}
