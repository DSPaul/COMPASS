using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Common.Models.Enums;

namespace COMPASS.Common.Models.Preferences
{
    public class UIState : ObservableObject
    {
        public CodexLayout StartupLayout { get; set; } = CodexLayout.Home;
        public string StartupCollection { get; set; } = "DefaultCollection";
        public int StartupTab { get; set; } = 0;

        public bool ShowCodexInfoPanel { get; set; } = true;
        public bool AutoHideCodexInfoPanel { get; set; } = true;

        public string SortProperty { get; set; } = nameof(Codex.Title);
        public ListSortDirection SortDirection { get; set; } = ListSortDirection.Ascending;
    }
}
