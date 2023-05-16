using COMPASS.ViewModels.Import;
using System.Windows;


namespace COMPASS.Windows
{
    /// <summary>
    /// Interaction logic for ImportFolderWindow.xaml
    /// </summary>
    public partial class ImportFolderWindow : Window
    {
        public ImportFolderWindow(ImportFolderViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
