using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace COMPASS.Resources.Controls
{
    /// <summary>
    /// Interaction logic for CheckableContainer.xaml
    /// </summary>
    [ContentProperty(nameof(InnerContent))]
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

        // Using a DependencyProperty as the backing store for Header.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CheckTextProperty =
            DependencyProperty.Register(nameof(CheckText), typeof(string), typeof(CheckableContainer), new PropertyMetadata(""));


        public bool IsChecked
        {
            get => (bool)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsChecked.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(CheckableContainer), new FrameworkPropertyMetadata
            {
                DefaultValue = false,
                BindsTwoWayByDefault = true,
                DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });


        public object InnerContent
        {
            get => GetValue(InnerContentProperty);
            set => SetValue(InnerContentProperty, value);
        }

        // Using a DependencyProperty as the backing store for InnerContent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InnerContentProperty =
            DependencyProperty.Register(nameof(InnerContent), typeof(object), typeof(CheckableContainer));


    }
}
