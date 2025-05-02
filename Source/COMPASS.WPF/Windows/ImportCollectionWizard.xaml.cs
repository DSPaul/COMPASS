using System.Windows;
using COMPASS.ViewModels.Import;

namespace COMPASS.Windows
{
    /// <summary>
    /// Interaction logic for ImportCollectionWizard.xaml
    /// </summary>
    public partial class ImportCollectionWizard : Window
    {
        public ImportCollectionWizard(ImportCollectionViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.CloseAction = Close;
            viewModel.CancelAction = () => DialogResult = false;
        }

        private void Window_Closed(object sender, System.EventArgs e) => ((ImportCollectionViewModel)DataContext).OnWizardClosing();
    }
}
