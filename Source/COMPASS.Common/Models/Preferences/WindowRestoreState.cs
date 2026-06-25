using Avalonia.Controls;

namespace COMPASS.Common.Models.Preferences
{
    /// <summary>
    /// The state to restore the main window to after startup
    /// </summary>
    [Serializable]
    public class WindowRestoreState
    {
        public WindowState WindowState { get; set; } = WindowState.Maximized;
        public double Width { get; set; } = 1200;
        public double Height { get; set; } = 800;
        public int X { get; set; }
        public int Y { get; set; }
    }
}
