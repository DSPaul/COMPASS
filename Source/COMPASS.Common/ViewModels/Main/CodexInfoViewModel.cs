using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models.Filters;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Models.Preferences;

namespace COMPASS.Common.ViewModels.Main
{
    public class CodexInfoViewModel : ViewModelBase
    {
        private readonly UIState _uiState;

        public CodexInfoViewModel(UIState uiState)
        {
            _uiState = uiState;
        }

        private FiltersViewModel? _FilterVm => TabsViewModel.GetInstance().ActiveTab?.FiltersVM;

        //whether the codex info panel is active
        public bool ShowCodexInfo
        {
            get => _uiState.ShowCodexInfoPanel;
            set
            {
                _uiState.ShowCodexInfoPanel = value;
                OnPropertyChanged(nameof(ShowInfo));
                OnPropertyChanged();
            }
        }

        //what the visibility is actually bound to
        public bool ShowInfo => AutoHide ? ShowCodexInfo && DisplayedCodex is not null : ShowCodexInfo;

        public bool HasDisplayedCodex => DisplayedCodex is not null;

        public CodexViewModel? DisplayedCodex
        {
            get;
            set
            {
                CodexViewModel? prevCodex = field;
                if (SetProperty(ref field, value))
                {
                    prevCodex?.DisposeCover();
                    field?.LoadCover();
                    OnPropertyChanged(nameof(ShowInfo));
                    OnPropertyChanged(nameof(HasDisplayedCodex));
                }
            }
        }

        public bool AutoHide
        {
            get => _uiState.AutoHideCodexInfoPanel;
            set
            {
                _uiState.AutoHideCodexInfoPanel = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowInfo));
            }
        }

        public RelayCommand ToggleCodexInfoCommand => field ??= new(() => ShowCodexInfo = !ShowCodexInfo);

        public RelayCommand<string> AddAuthorFilterCommand => field ??= new(AddAuthorFilter);
        private void AddAuthorFilter(string? author) => _FilterVm?.ActivateFilter(new AuthorFilter(author ?? ""));

        public RelayCommand<string> AddPublisherFilterCommand => field ??= new(AddPublisherFilter);
        private void AddPublisherFilter(string? publisher) => _FilterVm?.ActivateFilter(new PublisherFilter(publisher ?? ""));

        public RelayCommand<TagViewModel> AddTagFilterCommand => field ??= new(AddTagFilter);
        private void AddTagFilter(TagViewModel? tagVm)
        {
            if (tagVm == null) return;
            _FilterVm?.ActivateFilter(new TagFilter(tagVm));
        }
    }

    [Factory]
    public class CodexInfoViewModelFactory(IPreferencesService preferencesService)
    {
        public CodexInfoViewModel Create() => new(preferencesService.Preferences.UIState);
    }
}
