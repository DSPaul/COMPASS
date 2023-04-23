using COMPASS.Tools;
using COMPASS.ViewModels;
using Microsoft.Web.WebView2.Core;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace COMPASS.Windows
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow(SettingsViewModel vm, string tab = "")
        {
            VM = vm;
            DataContext = vm;
            vm.Refresh();
            InitializeComponent();
            LoadChangeLog();

            //jump to tab
            var TabItems = SettingsTabControl.Items;
            foreach (TabItem item in TabItems)
            {
                if ((string)item.Header == tab)
                {
                    SettingsTabControl.SelectedItem = item;
                    break;
                }
            }
        }

        private SettingsViewModel VM;

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) => VM.SavePreferences();

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        public async void LoadChangeLog()
        {
            string sHTML = "<!DOCTYPE html>" +
           "<html>" +
           "<head>" +
               "<meta charset=\"utf-8\" />" +
               "<title>Changelog</title>" +
           "</head>" +
           "<body>" +
               "<script src=\"https://emgithub.com/embed-v2.js?target=https%3A%2F%2Fgithub.com%2FDSPaul%2FCOMPASS%2Fblob%2Fmaster%2FChangelog.md&style=default&type=markdown&showFullPath=on\"></script>" +
           "</body>" +
           "</html>";

            await ChangelogWebView.EnsureCoreWebView2Async(await CoreWebView2Environment.CreateAsync(userDataFolder: VM.WebViewDataDir));
            ChangelogWebView.NavigateToString(sHTML);
        }

        private void SelectFolderForFolderTagPairButton_Click(object sender, RoutedEventArgs e) => NewFolderTagPairTextBox.Text = Utils.PickFolder();
    }
}
