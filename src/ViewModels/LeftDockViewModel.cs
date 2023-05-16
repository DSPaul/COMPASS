using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.ViewModels.Import;

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
        private RelayCommand<ImportSource> _importCommand;
        public RelayCommand<ImportSource> ImportCommand => _importCommand ??= new(source =>
        {
            ImportViewModel.Stealth = false;
            ImportViewModel.Import(source);
        });
        #endregion

    }
}
