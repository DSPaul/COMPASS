using CommunityToolkit.Mvvm.ComponentModel;
using COMPASS.ViewModels.Layouts;
using System.ComponentModel;

namespace COMPASS.Models.Preferences
{
    public class UIState : ObservableObject
    {
        public LayoutViewModel.Layout StartupLayout
        {
            get => (LayoutViewModel.Layout)Properties.Settings.Default.PreferedLayout;
            set => Properties.Settings.Default.PreferedLayout = (int)value;
        }
        public string StartupCollection
        {
            get => Properties.Settings.Default.StartupCollection;
            set => Properties.Settings.Default.StartupCollection = value;
        }
        public int StartupTab
        {
            get => Properties.Settings.Default.SelectedTab;
            set => Properties.Settings.Default.SelectedTab = value;
        }

        public bool ShowCodexInfoPanel
        {
            get => Properties.Settings.Default.ShowCodexInfo;
            set => Properties.Settings.Default.ShowCodexInfo = value;
        }
        public bool AutoHideCodexInfoPanel
        {
            get => Properties.Settings.Default.AutoHideCodexInfo;
            set => Properties.Settings.Default.AutoHideCodexInfo = value;
        }

        public string SortProperty
        {
            get => Properties.Settings.Default.SortProperty;
            set => Properties.Settings.Default.SortProperty = value;
        }
        public ListSortDirection SortDirection
        {
            get => (ListSortDirection)Properties.Settings.Default.SortDirection;
            set => Properties.Settings.Default.SortDirection = (int)value;
        }
    }
}
