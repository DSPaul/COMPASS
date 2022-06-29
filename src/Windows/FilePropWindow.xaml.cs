using System;
using System.IO;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using COMPASS.Models;
using COMPASS.ViewModels;

namespace COMPASS
{
    /// <summary>
    /// Interaction logic for FilePropWindow.xaml
    /// </summary>
    public partial class FilePropWindow : Window
    {
        //Constructor
        public FilePropWindow(CodexEditViewModel vm)
        {
            DataContext = vm;
            InitializeComponent();
            ((CodexEditViewModel)DataContext).CloseAction = new Action(this.Close);
        }

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MainGrid.Focus();
        }
    }
}
