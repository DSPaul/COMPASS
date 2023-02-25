using COMPASS.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;

namespace COMPASS.Windows
{
    /// <summary>
    /// Interaction logic for CodexBulkEditWindow.xaml
    /// </summary>
    public partial class CodexBulkEditWindow : Window
    {
        //Constructor
        public CodexBulkEditWindow(CodexBulkEditViewModel vm)
        {
            DataContext = vm;
            InitializeComponent();
            ((CodexBulkEditViewModel)DataContext).CloseAction = new Action(this.Close);
        }

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e) => MainGrid.Focus();
    }
}
