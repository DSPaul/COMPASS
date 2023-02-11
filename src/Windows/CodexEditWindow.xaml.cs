using COMPASS.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;


namespace COMPASS.Windows
{
    /// <summary>
    /// Interaction logic for CodexEditWindow.xaml
    /// </summary>
    public partial class CodexEditWindow : Window
    {
        //Constructor
        public CodexEditWindow(CodexEditViewModel vm)
        {
            DataContext = vm;
            InitializeComponent();
            ((CodexEditViewModel)DataContext).CloseAction = new Action(this.Close);

            AuthorsComboBox.ApplyTemplate();
        }

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e) => MainGrid.Focus();
    }
}
