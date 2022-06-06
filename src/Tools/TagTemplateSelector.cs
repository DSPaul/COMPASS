﻿using COMPASS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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