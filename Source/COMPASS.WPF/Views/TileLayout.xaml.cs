using System.Windows.Controls;
using System.Windows.Input;

namespace COMPASS.Views
{
    /// <summary>
    /// Interaction logic for TileLayout.xaml
    /// </summary>
    public partial class TileLayout : UserControl
    {
        public TileLayout()
        {
            InitializeComponent();
        }

        public void HandleDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem listBoxItem && listBoxItem.DataContext is Codex codex)
            {
                CodexViewModel.OpenCodex(codex);
            }
        }

        private void TileLayoutListBox_PreviewKeyDown(object sender, KeyEventArgs e) => CodexViewModel.ListBoxHandleKeyDown(sender, e);
    }
}
