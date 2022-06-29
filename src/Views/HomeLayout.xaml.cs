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
    /// Interaction logic for HomeLayout.xaml
    /// </summary>
    public partial class HomeLayout : UserControl
    {
        public HomeLayout()
        {
            InitializeComponent();
        }

        public void HandleDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Codex toOpen = ((ListBoxItem)sender).DataContext as Codex;
            CodexViewModel.OpenCodex(toOpen);
        }
    }
}
