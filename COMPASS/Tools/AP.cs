using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace COMPASS.Tools
{
    /// <summary>
    /// Attached Properties Class
    /// </summary>
    public sealed class AP : DependencyObject
    {

        public static PackIconKind GetIconKind(DependencyObject obj)
        {
            return (PackIconKind)obj.GetValue(IconKindProperty);
        }

        public static void SetIconKind(DependencyObject obj, PackIconKind value)
        {
            obj.SetValue(IconKindProperty, value);
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IconKindProperty =
            DependencyProperty.RegisterAttached("IconKind", typeof(PackIconKind), typeof(AP));
    }
}
