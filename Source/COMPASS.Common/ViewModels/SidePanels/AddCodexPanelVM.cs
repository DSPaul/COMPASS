using System.Threading.Tasks;
using Autofac;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.Views.Windows;

namespace COMPASS.Common.ViewModels.SidePanels
{
    public class AddCodexPanelVM
    {
        private AsyncRelayCommand<ImportSource>? _importCommand;
        public AsyncRelayCommand<ImportSource> ImportCommand => _importCommand ??= new(ImportViewModel.Import);

        private AsyncRelayCommand? _importBooksFromSatchelCommand;
        public AsyncRelayCommand ImportBooksFromSatchelCommand => _importBooksFromSatchelCommand ??= new(ImportBooksFromSatchel);
        public async Task ImportBooksFromSatchel()
        {
            var collectionStorageService = App.Container.Resolve<ICodexCollectionStorageService>();
            var collectionToImport = await collectionStorageService.OpenSatchel();

            if (collectionToImport == null)
            {
                Logger.Warn("Failed to open file");
                return;
            }

            //Create importCollection ready to merge into existing collection
            //set in advanced mode as a sort of preview
            var vm = new ImportCollectionViewModel(collectionToImport)
            {
                AdvancedImport = true,
                MergeIntoCollection = true
            };

            if (!vm.ContentSelectorVM.HasCodices)
            {
                Notification noItemsFound = new("No items found", $"{collectionToImport.Name[2..]} does not contain items to import");
                await App.Container.ResolveKeyed<INotificationService>(NotificationDisplayType.Windowed).Show(noItemsFound);
                return;
            }

            //setup for only codices
            vm.Steps.Clear();
            vm.Steps.Add(CollectionContentSelectorViewModel.ItemsStep);

            var w = new ImportCollectionWizard(vm);
            w.Show();
        }
    }
}
