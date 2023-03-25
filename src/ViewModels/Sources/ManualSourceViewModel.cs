using COMPASS.Models;
using COMPASS.Windows;
using System.Threading.Tasks;

namespace COMPASS.ViewModels.Sources
{
    public class ManualSourceViewModel : SourceViewModel
    {
        public ManualSourceViewModel() : base() { }
        public ManualSourceViewModel(CodexCollection targetCollection) : base(targetCollection) { }
        public override ImportSource Source => ImportSource.Manual;

        public override void Import()
        {
            IsImporting = true;

            CodexEditWindow editWindow = new(new CodexEditViewModel(null));
            editWindow.ShowDialog();
            editWindow.Topmost = true;
        }

        public override Task<Codex> SetMetaData(Codex codex) => Task.FromResult(codex);
        public override Task<bool> FetchCover(Codex codex) => Task.FromResult(false);
    }
}
