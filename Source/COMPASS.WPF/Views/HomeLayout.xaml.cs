using COMPASS.Models;
using COMPASS.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace COMPASS.Views
{
    /// <summary>
    /// Interaction logic for HomeLayout.xaml
    /// </summary>
    public partial class HomeLayout : UserControl
    {
        public HomeLayout()
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

        private void ListBox_PreviewKeyDown(object sender, KeyEventArgs e) => CodexViewModel.ListBoxHandleKeyDown(sender, e);
    }
}
