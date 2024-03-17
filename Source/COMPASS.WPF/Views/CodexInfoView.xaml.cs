using COMPASS.ViewModels;
using System.Windows;
using System.Windows.Controls;

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
