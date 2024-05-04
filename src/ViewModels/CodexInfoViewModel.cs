using CommunityToolkit.Mvvm.Input;
using COMPASS.Models;
using COMPASS.Services;

namespace COMPASS.ViewModels
{
    public class CodexInfoViewModel : ViewModelBase
    {

        public CodexInfoViewModel(MainViewModel mvm)
        {
            MVM = mvm;
            _preferencesService = PreferencesService.GetInstance();
        }

        private PreferencesService _preferencesService;

        //whether or not the codex info panel is active
        public bool ShowCodexInfo
        {
            get => _preferencesService.Preferences.UIState.ShowCodexInfoPanel;
            set
            {
                _preferencesService.Preferences.UIState.ShowCodexInfoPanel = value;
                OnPropertyChanged(nameof(ShowInfo));
                OnPropertyChanged();
            }
        }

        //what the visibility is actually bound to
        public bool ShowInfo => AutoHide ? ShowCodexInfo && MVM!.CurrentLayout.SelectedCodex is not null : ShowCodexInfo;
        public void SelectedItemChanged() => OnPropertyChanged(nameof(ShowInfo));

        public bool AutoHide
        {
            get => _preferencesService.Preferences.UIState.AutoHideCodexInfoPanel;
            set
            {
                _preferencesService.Preferences.UIState.AutoHideCodexInfoPanel = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowInfo));
            }
        }

        private RelayCommand? _toggleCodexInfoCommand;
        public RelayCommand ToggleCodexInfoCommand => _toggleCodexInfoCommand ??= new(() => ShowCodexInfo = !ShowCodexInfo);

        private RelayCommand<string>? _addAuthorFilterCommand;
        public RelayCommand<string> AddAuthorFilterCommand => _addAuthorFilterCommand ??= new(AddAuthorFilter);
        private void AddAuthorFilter(string? author) => MainViewModel.CollectionVM.FilterVM.AddFilter(new(Filter.FilterType.Author, author));

        private RelayCommand<string>? _addPublisherFilterCommand;
        public RelayCommand<string> AddPublisherFilterCommand => _addPublisherFilterCommand ??= new(AddPublisherFilter);
        private void AddPublisherFilter(string? publisher) => MainViewModel.CollectionVM.FilterVM.AddFilter(new(Filter.FilterType.Publisher, publisher));

        private RelayCommand<Tag>? _addTagFilterCommand;
        public RelayCommand<Tag> AddTagFilterCommand => _addTagFilterCommand ??= new(AddTagFilter);
        private void AddTagFilter(Tag? tag) => MainViewModel.CollectionVM.FilterVM.AddFilter(new(Filter.FilterType.Tag, tag));
    }
}
