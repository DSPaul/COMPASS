using COMPASS.ViewModels.Sources;
using System.Windows;

namespace COMPASS.Windows
{
    /// <summary>
    /// Interaction logic for ImportURLWindow.xaml
    /// </summary>
    public partial class ImportURLWindow : Window
    {
        public ImportURLWindow(OnlineSourceViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
