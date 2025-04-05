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

            string msg = codex == null ? "Failed to load thumbnail" : $"Failed to load thumbnail for {codex.Title}";
            msg += " It might be corrupted, try regenerating it";

            Logger.Error(msg, e.ErrorException);
            try
            {
                if (img != null)
                {
                    BitmapImage placeholder = new();
                    placeholder.BeginInit();
                    placeholder.UriSource = new Uri("pack://application:,,,/Media/CoverPlaceholder.png", UriKind.Absolute);
                    placeholder.EndInit();
                    placeholder.Freeze();

                    img.Source = placeholder;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error while handling thumbnail failure", ex);

                // As a last resort, try an even more basic approach
                try
                {
                    if (sender is Image fallbackImg)
                    {
                        // Try with a different path format as a last resort
                        string? assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
                        string fallbackPath = $"pack://application:,,,/{assemblyName};component/Media/CoverPlaceholder.png";
                        fallbackImg.Source = new BitmapImage(new Uri(fallbackPath));
                    }
                }
                catch (Exception innerEx)
                {
                    // At this point, we can't do much more than ensure we don't crash
                    Logger.Error("Critical failure in thumbnail error handler - unable to load any placeholder", innerEx);
                }
            }
        }
    }
}

