using System;
using System.Windows;
using System.Windows.Input;

namespace COMPASS.Resources.Controls
{
    /// <summary>
    /// Interaction logic for YesNoSelector.xaml
    /// </summary>
    public partial class YesNoSelector
    {
        public YesNoSelector()
        {
            InitializeComponent();
        }

        //command with parameters (bool addFilter/removeFilter, bool Invert)
        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        // Using a DependencyProperty as the backing store for Command.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(YesNoSelector), new PropertyMetadata());

        private void YesRadioBtn_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            Command.Execute(YesRadioBtn.IsChecked == false
                ? new Tuple<bool, bool>(true, false)
                : new Tuple<bool, bool>(false, false));
        }

        private void NoRadioBtn_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            Command.Execute(NoRadioBtn.IsChecked == false
                ? new Tuple<bool, bool>(true, true)
                : new Tuple<bool, bool>(false, true));
        }
    }
}
