using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Models;
using COMPASS.Common.Services;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.Views.Windows;
using System.Threading.Tasks;

namespace COMPASS.Common.ViewModels
{
    public class LeftDockViewModel : ViewModelBase, IDealsWithTabControl
    {
        public LeftDockViewModel(MainViewModel mainViewModel)
        {
            _mainVM = mainViewModel;
        }

        private MainViewModel _mainVM;
        public MainViewModel MainVM
        {
            get => _mainVM;
            init => SetProperty(ref _mainVM, value);
        }

        public int SelectedTab
        {
            get => Properties.Settings.Default.SelectedTab;
            set
            {
                Properties.Settings.Default.SelectedTab = value;
                OnPropertyChanged();
                if (value > 0) Collapsed = false;
            }
        }

        private bool _collapsed = false;
        public bool Collapsed
        {
            get => _collapsed;
            set
            {
                SetProperty(ref _collapsed, value);
                if (value) SelectedTab = 0;
            }
        }

        #region Add Books Tab
        private AsyncRelayCommand<ImportSource>? _importCommand;
        public AsyncRelayCommand<ImportSource> ImportCommand => _importCommand ??= new(ImportViewModel.Import);

        private AsyncRelayCommand? _importBooksFromSatchelCommand;
        public AsyncRelayCommand ImportBooksFromSatchelCommand => _importBooksFromSatchelCommand ??= new(ImportBooksFromSatchel);
        public async Task ImportBooksFromSatchel()
        {
            var collectionToImport = await IOService.OpenSatchel();

            if (collectionToImport == null)
            {
                Logger.Warn("Failed to open file");
                return;
            }

            //Create importcollection ready to merge into existing collection
            //set in advanced mode as a sort of preview
            var vm = new ImportCollectionViewModel(collectionToImport)
            {
                AdvancedImport = true,
                MergeIntoCollection = true
            };

            if (!vm.ContentSelectorVM.HasCodices)
            {
                messageDialog.Show($"{collectionToImport.DirectoryName[2..]} does not contain items to import", "No items found", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            //setup for only codices
            vm.Steps.Clear();
            vm.Steps.Add(CollectionContentSelectorViewModel.ItemsStep);

            var w = new ImportCollectionWizard(vm);
            w.Show();
        }
        #endregion

    }
}
