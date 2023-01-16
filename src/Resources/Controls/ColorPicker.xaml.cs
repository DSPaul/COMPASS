using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace COMPASS.Resources.Controls
{
    /// <summary>
    /// Interaction logic for ColorPicker.xaml
    /// </summary>
    public partial class ColorPicker : UserControl
    {
        public ColorPicker()
        {
            InitializeComponent();
        }

        public System.Windows.Media.Color SelectedColor
        {
            get { return (System.Windows.Media.Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(System.Windows.Media.Color), typeof(ColorPicker), new PropertyMetadata(Colors.Red));

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            SelectedColor = ((SolidColorBrush)btn.Background).Color;
        }
    }
}
