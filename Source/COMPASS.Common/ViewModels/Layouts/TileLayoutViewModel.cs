using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Preferences;
using COMPASS.Common.Operations;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.ViewModels.Main;

namespace COMPASS.Common.ViewModels.Layouts
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
            preferencesService.Preferences.TileLayoutPreferences, codexInfoVmFactory,
            importFilesVmFactory, collectionOperations, tabVm);
    }
}
