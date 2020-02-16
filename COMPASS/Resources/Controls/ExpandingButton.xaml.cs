using COMPASS.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace COMPASS.Resources.Controls
{
    /// <summary>
    /// Interaction logic for ExpandingButton.xaml
    /// </summary>
    public partial class ExpandingButton : UserControl
    {
        public ExpandingButton()
        {
            InitializeComponent();
        }

        public object Toggle
        {
            get { return (object)GetValue(ToggleProperty); }
            set { SetValue(ToggleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Toggle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ToggleProperty =
            DependencyProperty.Register("Toggle", typeof(object), typeof(ExpandingButton), new PropertyMetadata(0));



        public object HiddenContent
        {
            get { return (object)GetValue(HiddenContentProperty); }
            set { SetValue(HiddenContentProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HiddenContent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HiddenContentProperty =
            DependencyProperty.Register("HiddenContent", typeof(object), typeof(ExpandingButton), new PropertyMetadata(0));

        //Ischecked not working as it should when content of togglebtn is also a btn, This fixes that
        private void CheckedChanged(object sender, RoutedEventArgs e)
        {
            ToggleBtn.IsChecked = !ToggleBtn.IsChecked;
        }
    }
}
