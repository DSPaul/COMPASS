using COMPASS.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;


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

            AuthorsComboBox.ApplyTemplate();
        }

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e) => MainGrid.Focus();
    }
}
