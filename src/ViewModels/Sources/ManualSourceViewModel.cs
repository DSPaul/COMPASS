using COMPASS.Models;
using COMPASS.Windows;

namespace COMPASS.ViewModels
{
    public class ManualSourceViewModel : SourceViewModel
    {
        public override void Import()
        {
            CodexEditWindow editWindow = new(new CodexEditViewModel(null));
            editWindow.ShowDialog();
            editWindow.Topmost = true;
        }

        public override Codex SetMetaData(Codex codex) => codex;
    }
}
