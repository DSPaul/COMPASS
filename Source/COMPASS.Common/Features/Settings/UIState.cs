using COMPASS.Common.Features.Layouts;
using COMPASS.Infra.Preferences;
using System.ComponentModel;
using COMPASS.Common.Domain;

namespace COMPASS.Common.Features.Settings
{
    [Serializable]
    public class UIState : IPreferences
    {
        public CodexLayout StartupLayout { get; set; } = CodexLayout.Home;
        public string StartupCollection { get; set; } = Constants.DEFAULT_COLLECTION_NAME;
        public int StartupTab { get; set; } = -1;

        public bool ShowCodexInfoPanel { get; set; } = true;
        public bool AutoHideCodexInfoPanel { get; set; } = true;

        public string SortProperty { get; set; } = nameof(Codex.SortingTitle);
        public ListSortDirection SortDirection { get; set; } = ListSortDirection.Ascending;
    }
}
