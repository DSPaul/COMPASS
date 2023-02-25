using COMPASS.Models;
using COMPASS.Windows;

namespace COMPASS.ViewModels.Sources
{
    public class ManualSourceViewModel : SourceViewModel
    {
        public override ImportSource Source => ImportSource.Manual;

        public override void Import()
        {
            CodexEditWindow editWindow = new(new CodexEditViewModel(null));
            editWindow.ShowDialog();
            editWindow.Topmost = true;
        }

        public override Codex SetMetaData(Codex codex) => codex;
        public override bool FetchCover(Codex codex) => false;
    }
}
