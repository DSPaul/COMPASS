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
    /// Interaction logic for FileTileView.xaml
    /// </summary>
    public partial class FileTileView : UserControl
    {
        public FileTileView()
        {
            InitializeComponent();
        }

        public void HandleDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ((FileBaseViewModel)DataContext).OpenSelectedFile();
        }

        //Make sure selected Item is always in view
        private void FileView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox lb = sender as ListBox;
            if (lb != null && e.AddedItems != null && e.AddedItems.Count > 0)
            {
                lb.ScrollIntoView(e.AddedItems[0]);
            }
        }
    }
}
