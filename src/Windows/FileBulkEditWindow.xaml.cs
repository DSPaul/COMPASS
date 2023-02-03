using COMPASS.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;

namespace COMPASS
{
    /// <summary>
    /// Interaction logic for FileBulkEditWindow.xaml
    /// </summary>
    public partial class FileBulkEditWindow : Window
    {
        //Constructor
        public FileBulkEditWindow(CodexBulkEditViewModel vm)
        {
            DataContext = vm;
            InitializeComponent();
            ((CodexBulkEditViewModel)DataContext).CloseAction = new Action(this.Close);
        }

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e) => MainGrid.Focus();
    }
}
