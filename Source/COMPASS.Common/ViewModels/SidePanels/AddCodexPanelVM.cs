using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Exceptions;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.Modals.Import;
using COMPASS.Common.ViewModels.Selection;
using COMPASS.Common.Views.Windows;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.ViewModels.SidePanels
{
    public class AddCodexPanelVM : ViewModelBase
    {
        private AsyncRelayCommand<ImportSource>? _importCommand;
        public AsyncRelayCommand<ImportSource> ImportCommand => _importCommand ??= new(Import);

        private async Task Import(ImportSource source)
        {
            var importVm = new ImportViewModel( 
                TabsViewModel.GetInstance().ActiveTab?.CollectionVM.Identifier
                ?? throw new NoTabException("No tab was found, so no collection to import to"));
            await importVm.Import(source);
        }

        private AsyncRelayCommand? _importBooksFromSatchelCommand;
        public AsyncRelayCommand ImportBooksFromSatchelCommand => _importBooksFromSatchelCommand ??= new(ImportBooksFromSatchel);
        public async Task ImportBooksFromSatchel()
        {
            //satchels store data in xml format
            var importService = ServiceResolver.Resolve<IImportExportService>();
            var extractedCollection = await importService.OpenSatchel();

            if (extractedCollection == null)
            {
                Logger.Warn("Failed to open file");
                return;
            }

            //Create importCollection ready to merge into an existing collection
            CodexCollectionVM toImportVm = new(extractedCollection, StorageStrategy.Xml);
            
            var targetCollectionVm = TabsViewModel.GetInstance().ActiveTab!.CollectionVM;
            var vm = new ImportCollectionViewModel(toImportVm, targetCollectionVm)
            {
                //set in advanced mode as a sort of preview
                AdvancedImport = true,
                MergeIntoCollection = true
            };

            if (!vm.ContentSelectorVM.HasCodices)
            {
                Notification noItemsFound = new("No items found", $"{extractedCollection.Name[2..]} does not contain items to import");
                await ServiceResolver.Resolve<INotificationService>().ShowDialog(noItemsFound);
                return;
            }

            //setup for only codices
            vm.Steps.Clear();
            vm.Steps.Add(CollectionContentSelectorViewModel.ItemsStep);

            var w = new ModalWindow(vm);
            w.Show();
        }
    }
}
