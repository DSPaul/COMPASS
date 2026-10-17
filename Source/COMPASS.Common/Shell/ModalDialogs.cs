using COMPASS.Common.Features.CodexCollections;
using COMPASS.Common.Features.Codices;
using COMPASS.Common.Features.ImportExport;
using COMPASS.Common.Features.ISBN;
using COMPASS.Common.Features.Metadata;
using COMPASS.Common.Features.Settings;
using COMPASS.Common.Features.Tags;
using COMPASS.Infra.Avalonia.Modal;

namespace COMPASS.Common.Shell;

/// <summary>
/// Maps every modal view-model to its view.
/// </summary>
public static class ModalDialogs
{
    public static void Register()
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

        // Wizards
        ModalWindow.Register<ImportCollectionViewModel, ImportCollectionWizard>();
        ModalWindow.Register<ExportCollectionViewModel, ExportCollectionWizard>();
    }
}
