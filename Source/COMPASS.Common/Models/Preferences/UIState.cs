using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.Common.ViewModels.Layouts;
using System.ComponentModel;

namespace COMPASS.Common.Models.Preferences
{
    public class UIState : ObservableObject
    {
        public LayoutViewModel.Layout StartupLayout { get; set; } = LayoutViewModel.Layout.Home;
        public string StartupCollection { get; set; } = "DefaultCollection";
        public int StartupTab { get; set; } = 0;

        public bool ShowCodexInfoPanel { get; set; } = true;
        public bool AutoHideCodexInfoPanel { get; set; } = true;

        public string SortProperty { get; set; } = nameof(Codex.Title);
        public ListSortDirection SortDirection { get; set; } = ListSortDirection.Ascending;
    }
}
