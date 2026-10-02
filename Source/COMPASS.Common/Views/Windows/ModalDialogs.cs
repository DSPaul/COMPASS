using COMPASS.Common.ViewModels.Modals;
using COMPASS.Common.ViewModels.Modals.Edit;
using COMPASS.Common.ViewModels.Modals.Import;
using COMPASS.Common.Views.Modals;
using COMPASS.Common.Views.Modals.Edit;
using COMPASS.Common.Views.Modals.Import;
using COMPASS.Infra.Avalonia.Modal;

namespace COMPASS.Common.Views.Windows;

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
