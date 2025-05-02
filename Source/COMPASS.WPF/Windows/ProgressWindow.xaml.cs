using System.Collections.Specialized;
using System.Windows;
using COMPASS.ViewModels;

namespace COMPASS.Windows
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        public ProgressWindow(int bars = 1)
        {
            InitializeComponent();
            DataContext = ProgressViewModel.GetInstance();
            ((INotifyCollectionChanged)LogsControl.Items).CollectionChanged += Logs_CollectionChanged;
            totalBars = bars;
            Closing += OnClosing;
        }

        private int barsDone = 0;
        private int totalBars = 1;

        private void Logs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                // scroll the new item into view   
                Scroller.ScrollToEnd();
            }
        }

        private void ProgressBar_ValueChanged(object? sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ProgBar.Value >= 100)
            {
                Close();
            }

            if (barsDone >= totalBars)
            {
                Close();
            }
        }

        private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Call activate explicitly because of bug 
            // https://stackoverflow.com/questions/3144004/wpf-app-loses-focus-completely-on-window-close
            Owner ??= Application.Current.MainWindow;
            Owner!.Activate();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
