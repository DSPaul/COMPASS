using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using COMPASS.Models;
using COMPASS.Models.Enums;
using COMPASS.ViewModels;

namespace COMPASS.Views
{
    /// <summary>
    /// Interaction logic for LeftDockView.xaml
    /// </summary>
    public partial class LeftDockView : UserControl
    {
        public LeftDockView()
        {
            InitializeComponent();
            ((INotifyCollectionChanged)LogsControl.Items).CollectionChanged += Logs_CollectionChanged;
        }

        private void TagTree_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem? treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

            if (treeViewItem != null)
            {
                MainViewModel.CollectionVM.TagsVM.ContextTag = ((TreeViewNode)treeViewItem.Header).Tag;
                e.Handled = true;
            }
        }

        private void Logs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var template = Logs.Template;

                if (sender is ItemCollection itemCollection)
                {
                    LogEntry entry = itemCollection.SourceCollection.Cast<LogEntry>().Last();
                    if (entry.Severity == Severity.Warning)
                    {
                        var WarningNotification = (Ellipse)template.FindName("WarningNotification", Logs);
                        WarningNotification.Visibility = Visibility.Visible;
                    }
                    else if (entry.Severity == Severity.Error)
                    {
                        var ErrorNotification = (Ellipse)template.FindName("ErrorNotification", Logs);
                        ErrorNotification.Visibility = Visibility.Visible;
                    }
                    // scroll the new item into view   
                    Scroller.ScrollToEnd();
                }
            }
        }

        static TreeViewItem? VisualUpwardSearch(DependencyObject? source)
        {
            while (source != null && source is not TreeViewItem)
                source = VisualTreeHelper.GetParent(source);

            return source as TreeViewItem;
        }

        private void Logs_GotFocus(object sender, RoutedEventArgs e)
        {
            var template = Logs.Template;
            var WarningNotification = (Ellipse)template.FindName("WarningNotification", Logs);
            WarningNotification.Visibility = Visibility.Collapsed;
            var ErrorNotification = (Ellipse)template.FindName("ErrorNotification", Logs);
            ErrorNotification.Visibility = Visibility.Collapsed;
        }

        private void TabItemClick(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var tabItem = border!.TemplatedParent as TabItem;
            var tabControl = tabItem!.Parent as TabControl;
            var vm = tabControl!.DataContext as IDealsWithTabControl;
            if (tabItem.IsSelected)
            {
                vm!.SelectedTab = 0;
                e.Handled = true;
            }
        }

        private void Toggle_ContextMenu(object sender, RoutedEventArgs e)
        {
            ((Button)sender).ContextMenu!.PlacementTarget = (Button)sender;
            ((Button)sender).ContextMenu!.IsOpen = !((Button)sender).ContextMenu!.IsOpen;
        }
    }
}
