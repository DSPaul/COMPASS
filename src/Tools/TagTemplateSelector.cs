using COMPASS.Models;
using System.Windows;
using System.Windows.Controls;

namespace COMPASS.Tools
{
    public class TagTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement elemnt = container as FrameworkElement;
            TreeViewNode node = item as TreeViewNode;
            if (node.Tag.IsGroup)
            {
                return elemnt.FindResource("GroupTag") as HierarchicalDataTemplate;
            }
            else
            {
                return elemnt.FindResource("RegularTag") as HierarchicalDataTemplate;
            }
        }
    }
}
