using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Exceptions;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.ViewModels;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.Modals.Import;
using COMPASS.Common.ViewModels.Selection;
using COMPASS.Common.Views.Windows;
using COMPASS.Infra.Interfaces.Services;
using COMPASS.Infra.Models;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.ViewModels.SidePanels
{
    public class AddCodexPanelVM : ViewModelBase
    {
        private readonly IImportExportService _importExportService;
        private readonly INotificationService _notificationService;
        private readonly ImportViewModelFactory _importViewModelFactory;
        private readonly CodexCollectionVMFactory _codexCollectionVMFactory;
        private readonly ImportCollectionViewModelFactory _importCollectionViewModelFactory;

        public AddCodexPanelVM(
            IImportExportService importExportService,
            INotificationService notificationService,
            ImportViewModelFactory importViewModelFactory,
            CodexCollectionVMFactory codexCollectionVMFactory,
            ImportCollectionViewModelFactory importCollectionViewModelFactory)
        {
            _importExportService = importExportService;
            _notificationService = notificationService;
            _importViewModelFactory = importViewModelFactory;
            _codexCollectionVMFactory = codexCollectionVMFactory;
            _importCollectionViewModelFactory = importCollectionViewModelFactory;
        }

        private AsyncRelayCommand<ImportSource>? _importCommand;
        public AsyncRelayCommand<ImportSource> ImportCommand => _importCommand ??= new(Import);

        private async Task Import(ImportSource source)
        {
            var importVm = _importViewModelFactory.Create(
                TabsViewModel.GetInstance().ActiveTab?.CollectionVM.Identifier
                ?? throw new NoTabException("No tab was found, so no collection to import to"));
            await importVm.Import(source);
        }

        private AsyncRelayCommand? _importBooksFromSatchelCommand;
        public AsyncRelayCommand ImportBooksFromSatchelCommand => _importBooksFromSatchelCommand ??= new(ImportBooksFromSatchel);
        public async Task ImportBooksFromSatchel()
        {
            //satchels store data in xml format
            var importService = _importExportService;
            var extractedCollection = await importService.OpenSatchel();

            if (extractedCollection == null)
            {
                Logger.Warn("Failed to open file");
                return;
            }

            //Create importCollection ready to merge into an existing collection
            CodexCollectionVM toImportVm = _codexCollectionVMFactory.Create(extractedCollection, StorageStrategy.Xml);
            
            var targetCollectionVm = TabsViewModel.GetInstance().ActiveTab!.CollectionVM;
            var vm = _importCollectionViewModelFactory.Create(toImportVm, targetCollectionVm);
            //set in advanced mode as a sort of preview
            vm.AdvancedImport = true;
            vm.MergeIntoCollection = true;

            if (!vm.ContentSelectorVM.HasCodices)
            {
                Notification noItemsFound = new("No items found", $"{extractedCollection.Name[2..]} does not contain items to import");
                await _notificationService.ShowDialog(noItemsFound);
                return;
            }

            //setup for only codices
            vm.Steps.Clear();
            vm.Steps.Add(CollectionContentSelectorViewModel.ItemsStep);

            var w = new ModalWindow(vm);
            w.Show();
        }
    }

    [Factory]
    public class AddCodexPanelVMFactory(
        IImportExportService importExportService,
        INotificationService notificationService,
        ImportViewModelFactory importViewModelFactory,
        CodexCollectionVMFactory codexCollectionVMFactory,
        ImportCollectionViewModelFactory importCollectionViewModelFactory)
    {
        public AddCodexPanelVM Create()
            => new(importExportService, notificationService, importViewModelFactory, codexCollectionVMFactory, importCollectionViewModelFactory);
    }
}
