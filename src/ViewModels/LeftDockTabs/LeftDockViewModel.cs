using COMPASS.ViewModels.Commands;
using static COMPASS.Tools.Enums;

namespace COMPASS.ViewModels
{
    public class LeftDockViewModel : ViewModelBase
    {
        public LeftDockViewModel() : base()
        {
            TagsTabVM = new();
            FiltersTabVM = new();
        }


        #region Tags Tab
        private TagsTabViewModel _tagsTabVM;
        public TagsTabViewModel TagsTabVM
        {
            get { return _tagsTabVM; }
            set { SetProperty(ref _tagsTabVM, value); }
        }
        #endregion

        #region Filters Tab

        private FiltersTabViewModel _filtersTabVM;
        public FiltersTabViewModel FiltersTabVM
        {
            get { return _filtersTabVM; }
            set { SetProperty(ref _filtersTabVM, value); }
        }

        #endregion

        #region Layouts Tab
        //Change Fileview
        private RelayCommand<CodexLayout> _changeFileViewCommand;
        public RelayCommand<CodexLayout> ChangeFileViewCommand => _changeFileViewCommand ??= new(ChangeFileView);
        public void ChangeFileView(CodexLayout v)
        {
            Properties.Settings.Default.PreferedView = (int)v;
            MVM.CurrentLayout = v switch
            {
                CodexLayout.HomeLayout => new HomeLayoutViewModel(),
                CodexLayout.ListLayout => new ListLayoutViewModel(),
                CodexLayout.CardLayout => new CardLayoutViewModel(),
                CodexLayout.TileLayout => new TileLayoutViewModel(),
                _ => null
            };
        }
        #endregion

        #region Add Books Tab
        private ImportViewModel _currentImportVM;
        public ImportViewModel CurrentImportViewModel
        {
            get { return _currentImportVM; }
            set { SetProperty(ref _currentImportVM, value); }
        }

        private RelayCommand<Sources> _importFilesCommand;
        public RelayCommand<Sources> ImportFilesCommand => _importFilesCommand ??= new(ImportFiles);
        public void ImportFiles(Sources source)
        {
            CurrentImportViewModel = new ImportViewModel(source, MVM.CurrentCollection);
        }
        #endregion

    }
}
