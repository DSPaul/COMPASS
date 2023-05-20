using COMPASS.ViewModels;
using System.Windows;

namespace COMPASS.Windows
{
    /// <summary>
    /// Interaction logic for ChooseMetaDataWindow.xaml
    /// </summary>
    public partial class ChooseMetaDataWindow : Window
    {
        public ChooseMetaDataWindow(ChooseMetaDataViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            vm.CloseAction = Close;
        }
    }
}
