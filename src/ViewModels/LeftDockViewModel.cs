using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Services;
using COMPASS.Tools;
using COMPASS.ViewModels.Import;
using COMPASS.Windows;
using System.Threading.Tasks;

namespace COMPASS.ViewModels
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
                RaisePropertyChanged();
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
        private RelayCommand<ImportSource>? _importCommand;
        public RelayCommand<ImportSource> ImportCommand => _importCommand ??= new(async source => await ImportViewModel.Import(source));

        private ActionCommand? _importBooksFromCompassFileCommand;
        public ActionCommand ImportBooksFromCompassFileCommand => _importBooksFromCompassFileCommand ??= new(async () => await ImportBooksFromCompassFile());
        public async Task ImportBooksFromCompassFile()
        {
            var collectionToImport = await IOService.OpenSatchel();

            if (collectionToImport == null)
            {
                Logger.Warn("Failed to open file");
                return;
            }

            var vm = new ImportCollectionViewModel(collectionToImport)
            {
                //configure export vm for codices only
                AdvancedImport = true,
                MergeIntoCollection = true
            };

            if (!vm.ContentSelectorVM.HasCodices)
            {
                messageDialog.Show($"{collectionToImport.DirectoryName[2..]} does not contain items to import", "No items found", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            vm.Steps.Clear();
            vm.Steps.Add(CollectionContentSelectorViewModel.ItemsStep);
            vm.ContentSelectorVM.TagsSelectorVM.SelectedTagCollection!.TagsRoot.IsChecked = false;

            var w = new ImportCollectionWizard(vm);
            w.Show();
        }
        #endregion

    }
}
