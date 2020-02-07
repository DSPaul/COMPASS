using COMPASS.Models;
using COMPASS.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace COMPASS
{
    /// <summary>
    /// Interaction logic for TagPropWindow.xaml
    /// </summary>
    public partial class TagPropWindow : Window
    {
        public TagPropWindow(TagEditViewModel vm)
        {
            DataContext = vm;
            InitializeComponent();
            ((TagEditViewModel)DataContext).CloseAction = new Action(this.Close);
        }

        public void TagTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Tag selectedtag = (Tag)e.NewValue;
            if (selectedtag == null) return;
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
