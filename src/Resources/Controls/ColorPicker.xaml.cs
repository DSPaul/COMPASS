﻿using System.Windows;
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

        public Color SelectedColor
        {
            get => (Color)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for SelectedColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register(nameof(SelectedColor), typeof(Color), typeof(ColorPicker), new PropertyMetadata(Colors.Red));

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            SelectedColor = ((SolidColorBrush)btn.Background).Color;
        }
    }
}
