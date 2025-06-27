using COMPASS.Models;
using COMPASS.Services;
using COMPASS.Tools;
using COMPASS.ViewModels;
using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
            var tabItems = SettingsTabControl.Items;
            foreach (TabItem item in tabItems)
            {
                if ((string)item.Header == tab)
                {
                    SettingsTabControl.SelectedItem = item;
                    break;
                }
            }
        }

        private SettingsViewModel VM;

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) => VM.ApplyPreferences();

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        public async void LoadChangeLog()
        {
            try
            {
                string sHTML = "<!DOCTYPE html>" +
                   "<html>" +
                   "<head>" +
                       "<meta charset=\"utf-8\" />" +
                       "<title>Changelog</title>" +
                   "</head>" +
                   "<body>" +
                       "<script src=\"https://emgithub.com/embed-v2.js?target=https%3A%2F%2Fgithub.com%2FDSPaul%2FCOMPASS%2Fblob%2Fmaster%2FChangelog.md&style=a11y-dark&type=markdown&showFullPath=on\"></script>" +
                   "</body>" +
                   "</html>";

                var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: VM.WebViewDataDir);
                await ChangelogWebView.EnsureCoreWebView2Async(environment);
                ChangelogWebView.NavigateToString(sHTML);
            }
            catch (Exception ex)
            {
                Logger.Error("failed to load changelog", ex);
            }
        }

        private void SelectFolderForFolderTagPairButton_Click(object sender, RoutedEventArgs e) => NewFolderTagPairTextBox.Text = IOService.PickFolder();

        private void FilterOnlyNumbers(object sender, TextCompositionEventArgs e) => e.Handled = !Constants.RegexNumbersOnly().IsMatch(e.Text);

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var s = sender as ScrollViewer;
            if (s!.ComputedVerticalScrollBarVisibility == Visibility.Collapsed)
            {
                var parentScrollViewer = FindParentScrollViewer(s);
                if (parentScrollViewer is null) return;
                parentScrollViewer.ScrollToVerticalOffset(parentScrollViewer.VerticalOffset - e.Delta);
            }
        }

        private ScrollViewer? FindParentScrollViewer(DependencyObject child)
        {
            //get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            //end of visual tree, no ScrollViewer Found
            if (parentObject is null) return null;
            //check if the parent is scrollviewer and return if so
            return parentObject is ScrollViewer parent ? parent : FindParentScrollViewer(parentObject);
        }
    }
}
