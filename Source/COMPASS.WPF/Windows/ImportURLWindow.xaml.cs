using COMPASS.ViewModels.Import;
using System.Windows;

namespace COMPASS.Windows
{
    /// <summary>
    /// Interaction logic for ImportURLWindow.xaml
    /// </summary>
    public partial class ImportURLWindow : Window
    {
        public ImportURLWindow(ImportURLViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
