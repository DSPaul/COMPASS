using COMPASS.Common.Features.CodexCollections;
using COMPASS.Common.Features.Codices;
using COMPASS.Common.Features.ImportExport;
using COMPASS.Common.Features.ISBN;
using COMPASS.Common.Features.Layouts;
using COMPASS.Common.Features.Layouts.Card;
using COMPASS.Common.Features.Layouts.Home;
using COMPASS.Common.Features.Layouts.List;
using COMPASS.Common.Features.Layouts.Tile;
using COMPASS.Common.Features.Metadata;
using COMPASS.Common.Features.Settings;
using COMPASS.Common.Features.Tags;
using COMPASS.Common.Features.Onboarding;
using COMPASS.Infra.Avalonia.Modal;

namespace COMPASS.Common.Shell;

/// <summary>
/// Maps view-models to their views.
/// </summary>
internal static class ViewRegistrar
{
    internal static void RegisterModals()
    {
        // Modals
        ModalWindow.Register<CollectionEditViewModel, CollectionEditView>();
        ModalWindow.Register<TagEditViewModel, TagEditView>();
        ModalWindow.Register<CodexEditViewModel, CodexEditView>();
        ModalWindow.Register<CodexBulkEditViewModel, CodexBulkEditView>();
        ModalWindow.Register<ImportURLViewModel, ImportURLView>();
        ModalWindow.Register<ImportFolderViewModel, ImportFolderView>();
        ModalWindow.Register<ImportTagsViewModel, ImportTagsView>();
        ModalWindow.Register<ISBNScannerViewModel, ISBNScannerView>();
        ModalWindow.Register<FileNotFoundViewModel, FileNotFoundView>();
        ModalWindow.Register<SettingsViewModel, SettingsView>();
        ModalWindow.Register<ChangeDataLocationViewModel, ChangeDataLocationView>();
        ModalWindow.Register<ChooseMetadataViewModel, ChooseMetadataView>();
        ModalWindow.Register<V1toV2OnboardingVM, V1toV2OnboardingView>();

        // Wizards
        ModalWindow.Register<ImportCollectionViewModel, ImportCollectionWizard>();
        ModalWindow.Register<ExportCollectionViewModel, ExportCollectionWizard>();
    }

    internal static void RegisterLayouts()
    {
        CodexLayoutHost.Register<HomeLayoutViewModel, HomeLayout>();
        CodexLayoutHost.Register<ListLayoutViewModel, ListLayout>();
        CodexLayoutHost.Register<CardLayoutViewModel, CardLayout>();
        CodexLayoutHost.Register<TileLayoutViewModel, TileLayout>();
    }
}
