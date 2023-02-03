using COMPASS.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
            InitializeComponent();

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

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e) => MainGrid.Focus();

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) => VM.SavePreferences();

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
