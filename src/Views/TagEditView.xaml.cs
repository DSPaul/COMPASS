using COMPASS.Models;
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
    /// Interaction logic for TagEditView.xaml
    /// </summary>
    public partial class TagEditView : UserControl
    {
        public TagEditView()
        {
            InitializeComponent();
        }

        public void TagTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null) return;
            Tag selectedtag = ((TreeViewNode)e.NewValue).Tag;
            if (selectedtag == null) return;
            //if selection switches when tree is not visible: unwanted selection change, ignore
            if (ShowParentSelectionBtn.IsChecked == false) return;
            ((TagEditViewModel)DataContext).SelectedTag = selectedtag;
        }

        //makes objects lose focus when clicked away
        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MainGrid.Focus();
        }

        //make scroll with mousewheel work
        private void ParentSelection_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }
}
