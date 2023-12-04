using COMPASS.ViewModels;
using System.Windows;

namespace COMPASS.Windows
{
    /// <summary>
    /// Interaction logic for ExportCollectionWizard.xaml
    /// </summary>
    public partial class ExportCollectionWizard : Window
    {
        public ExportCollectionWizard(ExportCollectionViewModel vm)
        {
            DataContext = vm;
            InitializeComponent();
            vm.CloseAction = Close;
        }
    }
}
