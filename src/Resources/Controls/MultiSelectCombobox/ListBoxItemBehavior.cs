using System.Windows;
using System.Windows.Controls;

namespace BlackPearl.Controls.CoreLibrary.Behavior
{
    //References - http://stackoverflow.com/questions/1114092/listbox-scrollbar-doesnt-follow-selected-item-with-icollectionview
    //           - https://www.codeproject.com/Articles/28959/Introduction-to-Attached-Behaviors-in-WPF
    public static class ListBoxItemBehavior
    {
        public static readonly DependencyProperty IsBroughtIntoViewWhenSelectedProperty = DependencyProperty.RegisterAttached(
            name: "IsBroughtIntoViewWhenSelected",
            propertyType: typeof(bool),
            ownerType: typeof(ListBoxItemBehavior),
            defaultMetadata: new UIPropertyMetadata(false, OnIsBroughtIntoViewWhenSelectedChanged));
        public static bool GetIsBroughtIntoViewWhenSelected(ListBoxItem listBoxItem) => (bool)listBoxItem.GetValue(IsBroughtIntoViewWhenSelectedProperty);
        public static void SetIsBroughtIntoViewWhenSelected(ListBoxItem listBoxItem, bool value) => listBoxItem.SetValue(IsBroughtIntoViewWhenSelectedProperty, value);

        private static void OnIsBroughtIntoViewWhenSelectedChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            if (!(depObj is ListBoxItem item)
                || !(e.NewValue is bool broughtIntoViewWhenSelected))
            {
                return;
            }

            if (broughtIntoViewWhenSelected)
            {
                item.Selected += OnListBoxItemSelected;
            }
            else
            {
                item.Selected -= OnListBoxItemSelected;
            }
        }
        private static void OnListBoxItemSelected(object sender, RoutedEventArgs e)
        {
            // Only react to the Selected event raised by the ListBoxItem
            // whose IsSelected property was modified.  Ignore all ancestors
            // who are merely reporting that a descendant's Selected fired.
            if (!ReferenceEquals(sender, e.OriginalSource))
            {
                return;
            }

            if (e.OriginalSource is ListBoxItem item)
            {
                item.BringIntoView();
            }
        }
    }

    public static class ListBoxAttachedProperties
    {
        public static readonly DependencyProperty SelectionStartIndexProperty = DependencyProperty.RegisterAttached(
            name: "SelectionStartIndex",
            propertyType: typeof(int),
            ownerType: typeof(ListBoxItemBehavior),
            defaultMetadata: new UIPropertyMetadata(-1));
        public static int GetSelectionStartIndex(ListBox listBox) => (int)listBox.GetValue(SelectionStartIndexProperty);
        public static void SetSelectionStartIndex(ListBox listBox, int value) => listBox.SetValue(SelectionStartIndexProperty, value);

        public static readonly DependencyProperty SelectionEndIndexProperty = DependencyProperty.RegisterAttached(
            name: "SelectionEndIndex",
            propertyType: typeof(int),
            ownerType: typeof(ListBoxItemBehavior),
            defaultMetadata: new UIPropertyMetadata(-1));
        public static int GetSelectionEndIndex(ListBox listBox) => (int)listBox.GetValue(SelectionEndIndexProperty);
        public static void SetSelectioEndtIndex(ListBox listBox, int value) => listBox.SetValue(SelectionEndIndexProperty, value);
    }
}
