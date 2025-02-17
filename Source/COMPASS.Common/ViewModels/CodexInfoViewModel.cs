using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Filters;
using COMPASS.Common.Services;

namespace COMPASS.Common.ViewModels
{
    public class CodexInfoViewModel : ViewModelBase
    {

        public CodexInfoViewModel()
        {
            _preferencesService = PreferencesService.GetInstance();
        }

        private readonly PreferencesService _preferencesService;
        private Codex? _displayedCodex;

        //whether the codex info panel is active
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
        public bool ShowInfo => AutoHide ? ShowCodexInfo && DisplayedCodex is not null : ShowCodexInfo;

        public Codex? DisplayedCodex
        {
            get => _displayedCodex;
            set
            {
                Codex? prevCodex = _displayedCodex;
                if (SetProperty(ref _displayedCodex, value))
                {
                    prevCodex?.Cover?.Dispose();
                    _displayedCodex?.LoadCover();
                    OnPropertyChanged(nameof(ShowInfo));
                }
            }
        }

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
        private void AddAuthorFilter(string? author) => MainViewModel.CollectionVM.FilterVM.AddFilter(new AuthorFilter(author ?? ""));

        private RelayCommand<string>? _addPublisherFilterCommand;
        public RelayCommand<string> AddPublisherFilterCommand => _addPublisherFilterCommand ??= new(AddPublisherFilter);
        private void AddPublisherFilter(string? publisher) => MainViewModel.CollectionVM.FilterVM.AddFilter(new PublisherFilter(publisher ?? ""));

        private RelayCommand<Tag>? _addTagFilterCommand;
        public RelayCommand<Tag> AddTagFilterCommand => _addTagFilterCommand ??= new(AddTagFilter);
        private void AddTagFilter(Tag? tag) => MainViewModel.CollectionVM.FilterVM.AddFilter(new TagFilter(tag!));
    }
}
