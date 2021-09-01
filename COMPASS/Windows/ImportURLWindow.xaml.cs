﻿using COMPASS.Models;
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
    /// Interaction logic for ImportURLWindow.xaml
    /// </summary>
    public partial class ImportURLWindow : Window
    {
        public ImportURLWindow(ObservableObject vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}