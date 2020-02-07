using COMPASS.ViewModels;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace COMPASS.Views
{
    /// <summary>
    /// Interaction logic for Tag_FilterView.xaml
    /// </summary>
    public partial class Tag_FilterView : UserControl
    {
        public Tag_FilterView()
        {
            InitializeComponent();
        }
        public void TagTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Tag selectedtag = (Tag)e.NewValue;
            if (selectedtag == null) return;
            ((TagsFiltersViewModel)DataContext).MVM.FilterHandler.AddTagFilter(selectedtag);
            ((TagsFiltersViewModel)DataContext).SelectedTag = null;
        }

        private void TagTree_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

            if (treeViewItem != null)
            {
                ((TagsFiltersViewModel)DataContext).Context = treeViewItem.Header as Tag;
                e.Handled = true;
            }
        }

        static TreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
                source = VisualTreeHelper.GetParent(source);

            return source as TreeViewItem;
        }
    }
}
