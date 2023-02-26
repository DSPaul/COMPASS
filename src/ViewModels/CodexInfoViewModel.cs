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
    }
}
