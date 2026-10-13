using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Features.Filtering;
using COMPASS.Common.Features.Filtering.Filters;
using COMPASS.Common.Features.Tags;
using COMPASS.Common.Features.Settings;
using COMPASS.Infra.DependencyInjection;
using COMPASS.Infra.Preferences;
using COMPASS.Infra.Avalonia.Validation;
using COMPASS.Common.Features.Tabs;

namespace COMPASS.Common.Features.Codices
{
    public class CodexInfoViewModel : ValidatableViewModelBase
    {
        private readonly UIState _uiState;
        private readonly CollectionTabVM _tabVm;

        public CodexInfoViewModel(UIState uiState, CollectionTabVM tabVm)
        {
            _uiState = uiState;
            _tabVm = tabVm;
        }

        private FiltersViewModel _FilterVm => _tabVm.FiltersVM;

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
        private void AddAuthorFilter(string? author) => _FilterVm.ActivateFilter(new AuthorFilter(author ?? ""));

        public RelayCommand<string> AddPublisherFilterCommand => field ??= new(AddPublisherFilter);
        private void AddPublisherFilter(string? publisher) => _FilterVm.ActivateFilter(new PublisherFilter(publisher ?? ""));

        public RelayCommand<TagViewModel> AddTagFilterCommand => field ??= new(AddTagFilter);
        private void AddTagFilter(TagViewModel? tagVm)
        {
            if (tagVm == null) return;
            _FilterVm.ActivateFilter(new TagFilter(tagVm));
        }
    }

    [Factory]
    public class CodexInfoViewModelFactory(IPreferencesService preferencesService)
    {
        public CodexInfoViewModel Create(CollectionTabVM tabVm) => new(preferencesService.GetPreferences<UIState>(), tabVm);
    }
}
