using System.Windows;
using System.Windows.Controls;

namespace COMPASS.Resources.Controls
{
    /// <summary>
    /// Interaction logic for CheckableContainer.xaml
    /// </summary>
    public partial class CheckableContainer : UserControl
    {
        public CheckableContainer()
        {
            InitializeComponent();
        }

        public string CheckText
        {
            get => (string)GetValue(CheckTextProperty);
            set => SetValue(CheckTextProperty, value);
        }

        // Using a DependencyProperty as the backing store for CheckText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CheckTextProperty =
            DependencyProperty.Register("CheckText", typeof(string), typeof(CheckableContainer), new PropertyMetadata(""));


        public bool IsChecked
        {
            get => (bool)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsChecked.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register("IsChecked", typeof(bool), typeof(CheckableContainer), new PropertyMetadata(false));
    }
}
