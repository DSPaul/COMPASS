using COMPASS.Models;
using COMPASS.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
        }

        private void TagTree_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

            if (treeViewItem != null)
            {
                MainViewModel.CollectionVM.TagsVM.ContextTag = ((TreeViewNode)treeViewItem.Header).Tag;
                e.Handled = true;
            }
        }

        static TreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && source is not TreeViewItem)
                source = VisualTreeHelper.GetParent(source);

            return source as TreeViewItem;
        }
    }
}
