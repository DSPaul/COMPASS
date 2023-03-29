using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.ViewModels.Sources;

namespace COMPASS.ViewModels
{
    public class LeftDockViewModel : ObservableObject
    {
        public LeftDockViewModel(MainViewModel mainViewModel)
        {
            MainVM = mainViewModel;
        }

        private MainViewModel _mainVM;
        public MainViewModel MainVM
        {
            get => _mainVM;
            set => SetProperty(ref _mainVM, value);
        }

        public int SelectedTab
        {
            get => Properties.Settings.Default.SelectedTab;
            set
            {
                Properties.Settings.Default.SelectedTab = value;
                RaisePropertyChanged(nameof(SelectedTab));
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
                if (value == true) SelectedTab = 0;
            }
        }

        #region Add Books Tab
        private RelayCommand<ImportSource> _importCommand;
        public RelayCommand<ImportSource> ImportCommand => _importCommand ??= new(ImportFromSource);
        public void ImportFromSource(ImportSource source)
        {
            MainVM.ActiveSourceVM = SourceViewModel.GetSource(source);
            MainVM.ActiveSourceVM.Import();
        }
        #endregion

    }
}
