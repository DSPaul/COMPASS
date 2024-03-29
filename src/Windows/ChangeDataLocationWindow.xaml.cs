﻿using COMPASS.ViewModels;
using System.Windows;

namespace COMPASS.Windows
{
    /// <summary>
    /// Interaction logic for ChangeDataLocationWindow.xaml
    /// </summary>
    public partial class ChangeDataLocationWindow : Window
    {
        public ChangeDataLocationWindow(SettingsViewModel settingsViewModel)
        {
            InitializeComponent();
            DataContext = settingsViewModel;
        }

        private void Button_Click(object sender, RoutedEventArgs e) => Close();
    }
}
