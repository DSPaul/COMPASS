using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.ViewModels;
using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

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

        private void Thumbnail_ImageFailed(object sender, System.Windows.ExceptionRoutedEventArgs e)
        {
            Image? img = sender as Image;
            Codex? codex = img?.DataContext as Codex;

            string msg = codex == null ? "Failed to load tumbnail" : $"Failed to load tumbnail for {codex.Title}";

            Logger.Error(msg, e.ErrorException);

            if (img != null)
            {
                img.Source = new BitmapImage(new Uri("pack://application:,,,/Media/CoverPlaceholder.png"));
            }
        }
    }
}
