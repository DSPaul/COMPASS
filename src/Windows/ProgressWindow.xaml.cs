using COMPASS.ViewModels;
using System.Collections.Specialized;
using System.Windows;

namespace COMPASS.Windows
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        public ProgressWindow(ImportViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            ((INotifyCollectionChanged)LogsControl.Items).CollectionChanged += Logs_CollectionChanged;
            Closing += OnClosing;
        }

        private void Logs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                // scroll the new item into view   
                Scroller.ScrollToEnd();
            }
        }

        private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ProgBar.Value >= 100)
            {
                Close();
            }
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Call activate explicitly because of bug 
            // https://stackoverflow.com/questions/3144004/wpf-app-loses-focus-completely-on-window-close
            Owner ??= Application.Current.MainWindow;
            Owner.Activate();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
