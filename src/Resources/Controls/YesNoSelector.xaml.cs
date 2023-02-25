using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace COMPASS.Resources.Controls
{
    /// <summary>
    /// Interaction logic for YesNoSelector.xaml
    /// </summary>
    public partial class YesNoSelector : UserControl
    {
        public YesNoSelector()
        {
            InitializeComponent();
        }

        //command with parameters (bool addfilter/removefilter, bool Invert)
        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        // Using a DependencyProperty as the backing store for Command.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(YesNoSelector), new PropertyMetadata());

        private void YesRadioBtn_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (YesRadioBtn.IsChecked == false)
            {
                Command.Execute(new Tuple<bool, bool>(true, false));
            }
            else
            {
                Command.Execute(new Tuple<bool, bool>(false, false));
            }
        }

        private void NoRadioBtn_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (NoRadioBtn.IsChecked == false)
            {
                Command.Execute(new Tuple<bool, bool>(true, true));
            }
            else
            {
                Command.Execute(new Tuple<bool, bool>(false, true));
            }
        }
    }
}
