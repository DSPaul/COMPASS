using MaterialDesignThemes.Wpf;
using System.Windows;

namespace COMPASS.Tools
{
    /// <summary>
    /// Attached Properties Class
    /// </summary>
    public sealed class AP : DependencyObject
    {
        #region IconKind
        public static PackIconKind GetIconKind(DependencyObject obj)
        {
            return (PackIconKind)obj.GetValue(IconKindProperty);
        }

        public static void SetIconKind(DependencyObject obj, PackIconKind value)
        {
            obj.SetValue(IconKindProperty, value);
        }

        public static readonly DependencyProperty IconKindProperty =
            DependencyProperty.RegisterAttached("IconKind", typeof(PackIconKind), typeof(AP));
        #endregion

        #region PlaceHolderText
        public static string GetPlaceHolderText(DependencyObject obj)
        {
            return (string)obj.GetValue(PlaceHolderTextProperty);
        }

        public static void SetPlaceHolderText(DependencyObject obj, string value)
        {
            obj.SetValue(PlaceHolderTextProperty, value);
        }

        public static readonly DependencyProperty PlaceHolderTextProperty =
            DependencyProperty.RegisterAttached("PlaceHolderText", typeof(string), typeof(AP));
        #endregion
    }
}
