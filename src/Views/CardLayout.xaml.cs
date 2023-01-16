using COMPASS.Models;
using COMPASS.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace COMPASS.Views
{
    /// <summary>
    /// Interaction logic for CardLayout.xaml
    /// </summary>
    public partial class CardLayout : UserControl
    {
        public CardLayout()
        {
            InitializeComponent();
        }

        public void HandleDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Codex toOpen = ((ListBoxItem)sender).DataContext as Codex;
            CodexViewModel.OpenCodex(toOpen);
        }

        //Make sure selected Item is always in view
        private void FileView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox lb && e.AddedItems != null && e.AddedItems.Count > 0)
            {
                lb.ScrollIntoView(e.AddedItems[0]);
            }
        }
    }
}
