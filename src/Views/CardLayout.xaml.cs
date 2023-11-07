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

        private void CardLayoutListBox_PreviewKeyDown(object sender, KeyEventArgs e) => CodexViewModel.ListBoxHandleKeyDown(sender, e);
    }
}
