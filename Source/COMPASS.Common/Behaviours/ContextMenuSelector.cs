using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace COMPASS.Common.Behaviours
{
    public class ContextMenuSelector : AvaloniaObject
    {
        static ContextMenuSelector()
        {
            SingleSelectContextMenuProperty.Changed.AddClassHandler<Interactive>(Attach);
            MultiSelectContextMenuProperty.Changed.AddClassHandler<Interactive>(Attach);
        }
        
        public static readonly AttachedProperty<ContextMenu> SingleSelectContextMenuProperty =
            AvaloniaProperty.RegisterAttached<Control, ContextMenu>(
                "SingleSelectContextMenu", typeof(ContextMenuSelector));
        
        public static readonly AttachedProperty<ContextMenu> MultiSelectContextMenuProperty =
            AvaloniaProperty.RegisterAttached<Control, ContextMenu>(
                "MultiSelectContextMenu", typeof(ContextMenuSelector));

        public static void SetSingleSelectContextMenu(AvaloniaObject element, ContextMenu value) =>
            element.SetValue(SingleSelectContextMenuProperty, value);

        public static ContextMenu GetSingleSelectContextMenu(AvaloniaObject element) =>
            element.GetValue(SingleSelectContextMenuProperty);

        public static void SetMultiSelectContextMenu(AvaloniaObject element, ContextMenu value) =>
            element.SetValue(MultiSelectContextMenuProperty, value);

        public static ContextMenu GetMultiSelectContextMenu(AvaloniaObject element) =>
            element.GetValue(MultiSelectContextMenuProperty);

        // A flag to ensure the behavior attaches only once per control.
        private static readonly AttachedProperty<bool> IsAttachedProperty =
            AvaloniaProperty.RegisterAttached<Control, bool>("IsAttached", typeof(ContextMenuSelector));

        private static void Attach(Interactive interactElem, AvaloniaPropertyChangedEventArgs args)
        {
            // Prevent multiple attachments
            if (interactElem.GetValue(IsAttachedProperty))
                return;

            interactElem.SetValue(IsAttachedProperty, true);

            // Attach event handlers based on the type of control
            if (args.NewValue is ContextMenu)
            {
                switch (interactElem)
                {
                    case SelectingItemsControl selectingControl:
                        selectingControl.SelectionChanged += OnSelectionChanged;
                        break;
                    case DataGrid dataGrid:
                        dataGrid.SelectionChanged += OnSelectionChanged;
                        break;
                }
            }
        }

        private static void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            int itemCount = sender switch
            {
                ListBox listBox => listBox.SelectedItems?.Count ?? 0,
                DataGrid dataGrid => dataGrid.SelectedItems?.Count ?? 0,
                _ => 0
            };
            
            if (sender is Control control)
            {
                control.ContextMenu = itemCount switch
                {
                    1 => GetSingleSelectContextMenu(control),
                    > 1 => GetMultiSelectContextMenu(control),
                    _ => null
                };
            }
        }
        
    }
}
