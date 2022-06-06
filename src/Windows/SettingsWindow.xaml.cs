using COMPASS.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
            foreach(TabItem item in TabItems)
            {
                if ((string)item.Header == tab)
                {
                    SettingsTabControl.SelectedItem = item;
                    break;
                }
            }
        }

        private SettingsViewModel VM;

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MainGrid.Focus();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            VM.SavePreferences();
        }
    }
}
