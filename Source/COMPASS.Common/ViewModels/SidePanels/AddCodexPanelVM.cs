using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Exceptions;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.Selection;
using COMPASS.Common.Views.Windows;

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
            //satches store data in xml format
            var storageService = ServiceResolver.ResolveKeyed<ICodexCollectionStorageService>(StorageStrategy.Xml);
            var extractedCollectionName = await storageService.OpenSatchel();

            if (extractedCollectionName == null)
            {
                Logger.Warn("Failed to open file");
                return;
            }
            
            //Create importCollection ready to merge into an existing collection
            CodexCollection toImport = new(extractedCollectionName);
            CodexCollectionVM toImportVm = new(extractedCollectionName, toImport, storageService);
            var vm = new ImportCollectionViewModel(toImportVm)
            {
                //set in advanced mode as a sort of preview
                AdvancedImport = true,
                MergeIntoCollection = true
            };

            if (!vm.ContentSelectorVM.HasCodices)
            {
                Notification noItemsFound = new("No items found", $"{extractedCollectionName[2..]} does not contain items to import");
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
