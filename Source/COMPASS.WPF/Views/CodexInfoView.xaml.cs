using System.Windows;
using System.Windows.Controls;
using COMPASS.ViewModels;

namespace COMPASS.Views
{
    /// <summary>
    /// Interaction logic for CodexInfoView.xaml
    /// </summary>
    public partial class CodexInfoView : UserControl
    {
        public CodexInfoView()
        {
            InitializeComponent();
        }

        private void SelectedCodexChanged(object sender, DependencyPropertyChangedEventArgs e) => ((CodexInfoViewModel)DataContext).SelectedItemChanged();
    }
}
