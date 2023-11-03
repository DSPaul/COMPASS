using COMPASS.ViewModels.Import;
using System.Windows;

namespace COMPASS.Windows
{
    /// <summary>
    /// Interaction logic for ImportTagsWindow.xaml
    /// </summary>
    public partial class ImportTagsWindow : Window
    {
        public ImportTagsWindow(ImportTagsViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            vm.CloseAction = Close;
        }
    }
}
