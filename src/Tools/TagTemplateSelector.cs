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
            bool isGroup = false;
            if (node != null)
            {
                isGroup = node.Tag.IsGroup;
            }
            else
            {
                CheckableTreeNode<Tag> checknode = item as CheckableTreeNode<Tag>;
                isGroup = checknode.Item.IsGroup;
            }

            if (isGroup)
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
