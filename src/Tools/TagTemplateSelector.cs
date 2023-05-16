using System.Diagnostics;
using COMPASS.Models;
using System.Windows;
using System.Windows.Controls;

namespace COMPASS.Tools
{
    public class TagTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement element = container as FrameworkElement;
            TreeViewNode node = item as TreeViewNode;
            Debug.Assert(node != null, nameof(node) + " != null");
            if (node.Tag.IsGroup)
            {
                return element?.FindResource("GroupTag") as HierarchicalDataTemplate;
            }
            else
            {
                return element?.FindResource("RegularTag") as HierarchicalDataTemplate;
            }
        }
    }
}
