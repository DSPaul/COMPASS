using COMPASS.Models;
using COMPASS.ViewModels;
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
            Codex toOpen = ((ListBoxItem)sender).DataContext as Codex;
            CodexViewModel.OpenCodex(toOpen);
        }

        private void TileLayoutListBox_PreviewKeyDown(object sender, KeyEventArgs e) => CodexViewModel.ListBoxHandleKeyDown(sender, e);

        //Disabled scroll into view as it seems bugged, scrollregion that the object is scrolled into keeps going up
        private void ListBoxItem_RequestBringIntoView(object sender, System.Windows.RequestBringIntoViewEventArgs e) => e.Handled = true;
    }
}
