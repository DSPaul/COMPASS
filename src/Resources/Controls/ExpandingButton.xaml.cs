using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

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
            DependencyProperty.Register("Toggle", typeof(object), typeof(ExpandingButton), new PropertyMetadata(0));



        public object HiddenContent
        {
            get => GetValue(HiddenContentProperty);
            set => SetValue(HiddenContentProperty, value);
        }

        // Using a DependencyProperty as the backing store for HiddenContent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HiddenContentProperty =
            DependencyProperty.Register("HiddenContent", typeof(object), typeof(ExpandingButton), new PropertyMetadata(0));

        //Ischecked not working as it should when content of togglebtn is also a btn, This fixes that
        private void CheckedChanged(object sender, RoutedEventArgs e) => ToggleBtn.IsChecked = !ToggleBtn.IsChecked;
        bool animating = false;

        private void HiddenContentControl_LayoutUpdated(object sender, EventArgs e)
        {
            if (!animating)
            {
                animating = true;
                HiddenContentGrid.Measure(new Size(1920, 700));
                DoubleAnimation animHeight = new();
                DoubleAnimation animWidth = new();
                animHeight.From = OutsidePanel.ActualHeight;
                animWidth.From = OutsidePanel.ActualWidth;
                animHeight.To = HiddenContentGrid.DesiredSize.Height + ToggleBtn.ActualHeight;
                animWidth.To = Math.Max(HiddenContentGrid.DesiredSize.Width, ToggleBtn.ActualWidth);
                animHeight.Duration = new Duration(TimeSpan.FromSeconds(0.3));
                animWidth.Duration = new Duration(TimeSpan.FromSeconds(0.3));
                Storyboard.SetTarget(animHeight, OutsidePanel);
                Storyboard.SetTarget(animWidth, OutsidePanel);
                Storyboard.SetTargetProperty(animHeight, new PropertyPath(HeightProperty));
                Storyboard.SetTargetProperty(animWidth, new PropertyPath(WidthProperty));
                Storyboard st = new();
                st.Children.Add(animHeight);
                //st.Children.Add(animWidth);
                st.Completed += (a, b) => animating = false;
                st.Begin();
            }
        }
    }
}
