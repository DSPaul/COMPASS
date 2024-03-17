using System.Windows.Controls;
﻿using System.Windows;

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
            get => GetValue(ToggleProperty);
            set => SetValue(ToggleProperty, value);
        }

        // Using a DependencyProperty as the backing store for Toggle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ToggleProperty =
            DependencyProperty.Register(nameof(Toggle), typeof(object), typeof(ExpandingButton), new PropertyMetadata(0));



        public object HiddenContent
        {
            get => GetValue(HiddenContentProperty);
            set => SetValue(HiddenContentProperty, value);
        }

        // Using a DependencyProperty as the backing store for HiddenContent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HiddenContentProperty =
            DependencyProperty.Register(nameof(HiddenContent), typeof(object), typeof(ExpandingButton), new PropertyMetadata(0));

        //IsChecked not working as it should when content of toggleBtn is also a btn, This fixes that
        private void CheckedChanged(object sender, RoutedEventArgs e) => ToggleBtn.IsChecked = !ToggleBtn.IsChecked;
    }
}
