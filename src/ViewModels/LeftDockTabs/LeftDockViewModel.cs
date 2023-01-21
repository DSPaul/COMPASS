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
            ImportFilesCommand = new RelayCommand<Sources>(ImportFiles);
        }

        private TagsTabViewModel _tagsTabVM;
        public TagsTabViewModel TagsTabVM
        {
            get { return _tagsTabVM; }
            set { SetProperty(ref _tagsTabVM, value); }
        }

        private FiltersTabViewModel _filtersTabVM;
        public FiltersTabViewModel FiltersTabVM
        {
            get { return _filtersTabVM; }
            set { SetProperty(ref _filtersTabVM, value); }
        }

        //Import ViewModel
        private ImportViewModel _currentImportVM;
        public ImportViewModel CurrentImportViewModel
        {
            get { return _currentImportVM; }
            set { SetProperty(ref _currentImportVM, value); }
        }

        //Import Btn
        public RelayCommand<Sources> ImportFilesCommand { get; private set; }
        public void ImportFiles(Sources source)
        {
            CurrentImportViewModel = new ImportViewModel(source, MVM.CurrentCollection);
        }

    }
}
