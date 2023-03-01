using COMPASS.Commands;
using COMPASS.Models;

namespace COMPASS.ViewModels
{
    public class CodexInfoViewModel : ObservableObject
    {

        public CodexInfoViewModel(MainViewModel mvm)
        {
            MVM = mvm;
        }

        public MainViewModel MVM { get; init; }
        public bool ShowCodexInfo
        {
            get => Properties.Settings.Default.ShowCodexInfo;
            set
            {
                Properties.Settings.Default.ShowCodexInfo = value;
                RaisePropertyChanged(nameof(ShowCodexInfo));
            }
        }

        private ActionCommand _toggleCodexInfoCommand;
        public ActionCommand ToggleCodexInfoCommand => _toggleCodexInfoCommand ??= new(() => ShowCodexInfo = !ShowCodexInfo);

        private RelayCommand<string> _addAuthorFilterCommand;
        public RelayCommand<string> AddAuthorFilterCommand => _addAuthorFilterCommand ??= new(AddAuthorFilter);
        private void AddAuthorFilter(string author) => MainViewModel.CollectionVM.FilterVM.AddFilter(new(Filter.FilterType.Author, author));

        private RelayCommand<string> _addPublisherFilterCommand;
        public RelayCommand<string> AddPublisherFilterCommand => _addPublisherFilterCommand ??= new(AddPublisherFilter);
        private void AddPublisherFilter(string publisher) => MainViewModel.CollectionVM.FilterVM.AddFilter(new(Filter.FilterType.Publisher, publisher));

        private RelayCommand<Tag> _addTagFilterCommand;
        public RelayCommand<Tag> AddTagFilterCommand => _addTagFilterCommand ??= new(AddTagFilter);
        private void AddTagFilter(Tag tag) => MainViewModel.CollectionVM.FilterVM.AddFilter(new(Filter.FilterType.Tag, tag));
    }
}
