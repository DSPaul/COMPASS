using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Common.Commands;
using COMPASS.Common.Models;

namespace COMPASS.Common.ViewModels
{
    public class CodexInfoViewModel : ObservableObject
    {

        public CodexInfoViewModel(MainViewModel mvm)
        {
            MVM = mvm;
        }

        public MainViewModel MVM { get; }

        //whether or not the codex info panel is active
        public bool ShowCodexInfo
        {
            get => Properties.Settings.Default.ShowCodexInfo;
            set
            {
                Properties.Settings.Default.ShowCodexInfo = value;
                RaisePropertyChanged(nameof(ShowInfo));
                RaisePropertyChanged();
            }
        }

        //what the visibility is actually bound to
        public bool ShowInfo => AutoHide ? ShowCodexInfo && MVM.CurrentLayout.SelectedCodex is not null : ShowCodexInfo;
        public void SelectedItemChanged() => RaisePropertyChanged(nameof(ShowInfo));

        public bool AutoHide
        {
            get => Properties.Settings.Default.AutoHideCodexInfo;
            set
            {
                Properties.Settings.Default.AutoHideCodexInfo = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ShowInfo));
            }
        }

        private ActionCommand? _toggleCodexInfoCommand;
        public ActionCommand ToggleCodexInfoCommand => _toggleCodexInfoCommand ??= new(() => ShowCodexInfo = !ShowCodexInfo);

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
