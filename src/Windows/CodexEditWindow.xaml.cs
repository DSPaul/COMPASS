using COMPASS.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
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

        // https://serialseb.com/blog/2007/09/03/wpf-tips-6-preventing-scrollviewer-from/
        private void treeView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is TreeView && !e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((Control)sender).Parent as UIElement;
                parent.RaiseEvent(eventArg);
            }
        }
    }
}
