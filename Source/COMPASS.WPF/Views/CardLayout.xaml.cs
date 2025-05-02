using System.Windows.Controls;
using System.Windows.Input;
using COMPASS.Models;
using COMPASS.ViewModels;

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
            if (sender is ListBoxItem listBoxItem && listBoxItem.DataContext is Codex codex)
            {
                CodexViewModel.OpenCodex(codex);
            }
        }

        private void CardLayoutListBox_PreviewKeyDown(object sender, KeyEventArgs e) => CodexViewModel.ListBoxHandleKeyDown(sender, e);
    }
}
