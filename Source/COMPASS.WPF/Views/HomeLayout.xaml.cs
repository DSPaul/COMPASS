using System.Windows.Controls;
using System.Windows.Input;
using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.ViewModels;

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

        private void Thumbnail_ImageFailed(object sender, System.Windows.ExceptionRoutedEventArgs e)
        {
            Image? img = sender as Image;
            Codex? codex = img?.DataContext as Codex;

            string msg = codex == null ? "Failed to load thumbnail\n" : $"Failed to load thumbnail for {codex.Title}\n";
            msg += "It might be corrupted, try regenerating it from the edit window";

            Logger.Warn(msg, e.ErrorException);
        }
    }
}
