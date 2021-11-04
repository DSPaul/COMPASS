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
using System.Windows.Media.Animation;
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

            //if (HiddenContentGrid.Visibility == Visibility.Collapsed)
            //{
            //    HiddenContentGrid.Visibility = Visibility.Visible;
            //    HiddenContentGrid.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            //    DoubleAnimation animHeight = new DoubleAnimation();
            //    DoubleAnimation animWidth = new DoubleAnimation();
            //    animHeight.From = OutsidePanel.ActualHeight;
            //    animWidth.From = OutsidePanel.ActualWidth;
            //    animHeight.To = OutsidePanel.ActualHeight + HiddenContentGrid.DesiredSize.Height;
            //    animWidth.To = Math.Max(OutsidePanel.ActualWidth, HiddenContentGrid.DesiredSize.Width);
            //    animHeight.Duration = new Duration(TimeSpan.FromSeconds(0.25));
            //    animWidth.Duration = new Duration(TimeSpan.FromSeconds(0.15));
            //    Storyboard.SetTarget(animHeight, OutsidePanel);
            //    Storyboard.SetTarget(animWidth, OutsidePanel);
            //    Storyboard.SetTargetProperty(animHeight, new PropertyPath(HeightProperty));
            //    Storyboard.SetTargetProperty(animWidth, new PropertyPath(WidthProperty));
            //    Storyboard st = new Storyboard();
            //    st.Children.Add(animHeight);
            //    //st.Children.Add(animWidth);
            //    st.Begin();
            //}
            //else
            //{
            //    HiddenContentGrid.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            //    DoubleAnimation animHeight = new DoubleAnimation();
            //    DoubleAnimation animWidth = new DoubleAnimation();
            //    animHeight.From = OutsidePanel.ActualHeight;
            //    animWidth.From = OutsidePanel.ActualWidth;
            //    animHeight.To = OutsidePanel.ActualHeight - HiddenContentGrid.DesiredSize.Height;
            //    //animWidth.To = ((ContentControl)ToggleBtn.Template.FindName("ToggleBtnContent", ToggleBtn)).Width;
            //    //ToggleBtn.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            //    var parent = VisualTreeHelper.GetParent(ExpandingBtnControl) as Grid;
            //    parent.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            //    animWidth.To = parent.DesiredSize.Width;
            //    animHeight.Duration = new Duration(TimeSpan.FromSeconds(0.25));
            //    animWidth.Duration = new Duration(TimeSpan.FromSeconds(0.15));
            //    Storyboard.SetTarget(animHeight, OutsidePanel);
            //    Storyboard.SetTarget(animWidth, OutsidePanel);
            //    Storyboard.SetTargetProperty(animHeight, new PropertyPath(HeightProperty));
            //    Storyboard.SetTargetProperty(animWidth, new PropertyPath(WidthProperty));
            //    Storyboard st = new Storyboard();
            //    st.Children.Add(animHeight);
            //    //st.Children.Add(animWidth);
            //    st.Completed += (a, b) => { 
            //        HiddenContentGrid.Visibility = Visibility.Collapsed;
            //    };
            //    st.Begin();
            //}
        }

        bool animating = false;

        private void HiddenContentControl_LayoutUpdated(object sender, EventArgs e)
        {
            if (!animating)
            {
                animating = true;
                HiddenContentGrid.Measure(new Size(1920, 700));
                DoubleAnimation animHeight = new DoubleAnimation();
                DoubleAnimation animWidth = new DoubleAnimation();
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
                Storyboard st = new Storyboard();
                st.Children.Add(animHeight);
                //st.Children.Add(animWidth);
                st.Completed += (a, b) => { animating = false; };
                st.Begin();
            }           
        }
    }
}
