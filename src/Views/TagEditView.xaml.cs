using System.Windows.Controls;
using System.Windows.Input;

namespace COMPASS.Views
{
    /// <summary>
    /// Interaction logic for TagEditView.xaml
    /// </summary>
    public partial class TagEditView : UserControl
    {
        public TagEditView()
        {
            InitializeComponent();
        }

        //makes objects lose focus when clicked away
        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e) => MainGrid.Focus();
    }
}
