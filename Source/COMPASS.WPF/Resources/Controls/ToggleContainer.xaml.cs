using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace COMPASS.Resources.Controls
{
    /// <summary>
    /// Interaction logic for ToggleContainer.xaml
    /// </summary>
    public partial class ToggleContainer : UserControl
    {
        public ToggleContainer()
        {
            InitializeComponent();
        }

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        // Using a DependencyProperty as the backing store for Header.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(string), typeof(ToggleContainer), new PropertyMetadata(""));


        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsOpen.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(ToggleContainer), new FrameworkPropertyMetadata
            {
                DefaultValue = false,
                BindsTwoWayByDefault = true,
                DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
    }
}
