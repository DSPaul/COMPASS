using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Filters;
using COMPASS.Common.Services;
using COMPASS.Common.ViewModels.ModelVMs;

namespace COMPASS.Common.ViewModels.Main
{
    public class CodexInfoViewModel : ViewModelBase
    {

        public CodexInfoViewModel()
        {
            _preferencesService = PreferencesService.GetInstance();
        }
        
        private FiltersViewModel? _FilterVm => TabsViewModel.GetInstance().ActiveTab?.FiltersVM;

        private readonly PreferencesService _preferencesService;
        private CodexViewModel? _displayedCodex;

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

        public CodexViewModel? DisplayedCodex
        {
            get => _displayedCodex;
            set
            {
                CodexViewModel? prevCodex = _displayedCodex;
                if (SetProperty(ref _displayedCodex, value))
                {
                    prevCodex?.DisposeCover();
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
        private void AddAuthorFilter(string? author) => _FilterVm?.AddFilter(new AuthorFilter(author ?? ""));

        private RelayCommand<string>? _addPublisherFilterCommand;
        public RelayCommand<string> AddPublisherFilterCommand => _addPublisherFilterCommand ??= new(AddPublisherFilter);
        private void AddPublisherFilter(string? publisher) => _FilterVm?.AddFilter(new PublisherFilter(publisher ?? ""));

        private RelayCommand<TagViewModel>? _addTagFilterCommand;
        public RelayCommand<TagViewModel> AddTagFilterCommand => _addTagFilterCommand ??= new(AddTagFilter);
        private void AddTagFilter(TagViewModel? tagVm)
        {
            if (tagVm == null) return;
            _FilterVm?.AddFilter(new TagFilter(tagVm));
        }
    }
}
