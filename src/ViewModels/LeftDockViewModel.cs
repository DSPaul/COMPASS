using COMPASS.Commands;
using COMPASS.Models;


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
        private RelayCommand<SourceViewModel.Sources> _importCommand;
        public RelayCommand<SourceViewModel.Sources> ImportCommand => _importCommand ??= new(ImportFromSource);
        public void ImportFromSource(SourceViewModel.Sources source)
        {
            SourceViewModel Source = SourceViewModel.GetSource(source);
            Source.Import();
        }
        #endregion

    }
}
