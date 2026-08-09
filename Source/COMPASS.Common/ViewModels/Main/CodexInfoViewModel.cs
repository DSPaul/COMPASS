using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models.Filters;
using COMPASS.Infra.Tools;
using COMPASS.Common.ViewModels.ModelVMs;

namespace COMPASS.Common.ViewModels.Main
{
    public class CodexInfoViewModel : ViewModelBase
    {

        public CodexInfoViewModel()
        {
            _preferencesService = ServiceResolver.Resolve<IPreferencesService>();
        }

        private FiltersViewModel? _FilterVm => TabsViewModel.GetInstance().ActiveTab?.FiltersVM;

        private readonly IPreferencesService _preferencesService;

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
            get => _preferencesService.Preferences.UIState.AutoHideCodexInfoPanel;
            set
            {
                _preferencesService.Preferences.UIState.AutoHideCodexInfoPanel = value;
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
}
