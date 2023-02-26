using COMPASS.Models;

namespace COMPASS.ViewModels
{
    public abstract class LayoutViewModel : ViewModelBase
    {
        protected LayoutViewModel() : base() { }

        public enum Layout
        {
            List,
            Card,
            Tile,
            Home
        }

        // Should put this function seperate Factory class for propper factory pattern
        // but I don't see the point, seems a lot of boilerplate without real advantages
        public static LayoutViewModel GetLayout(Layout? layout = null)
        {
            layout ??= (Layout)Properties.Settings.Default.PreferedLayout;
            Properties.Settings.Default.PreferedLayout = (int)layout;
            LayoutViewModel newLayout = layout switch
            {
                Layout.Home => new HomeLayoutViewModel(),
                Layout.List => new ListLayoutViewModel(),
                Layout.Card => new CardLayoutViewModel(),
                Layout.Tile => new TileLayoutViewModel(),
                _ => null
            };
            return newLayout;
        }

        #region Properties

        public CodexViewModel CodexVM { get; init; } = new();

        //Selected File
        private Codex _selectedCodex;
        public Codex SelectedCodex
        {
            get => _selectedCodex;
            set => SetProperty(ref _selectedCodex, value);
        }

        //Set Type of view
        public Layout LayoutType { get; init; }
        #endregion
    }
}
