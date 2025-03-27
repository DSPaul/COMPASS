using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Metadata;
using COMPASS.Common.Models;
using System.Collections.Generic;

namespace COMPASS.Common.DataTemplates
{

    //based on https://github.com/AvaloniaUI/Avalonia.Samples/tree/main/src/Avalonia.Samples/DataTemplates/IDataTemplateSample

    public class TagTreeDataTemplateSelector : ITreeDataTemplate
    {
        // This Dictionary should store our templates. We mark this as [Content], so we can directly add elements to it later.
        [Content]
        public Dictionary<string, ITreeDataTemplate> AvailableTemplates { get; } = [];

        public Control? Build(object? param) => GetTemplate(param).Build(param);
        public InstancedBinding? ItemsSelector(object item) => GetTemplate(item).ItemsSelector(item);

        // Check if we can accept the provided data
        public bool Match(object? data) => data is TreeNode || data is CheckableTreeNode<Tag> || data is Tag;

        private ITreeDataTemplate GetTemplate(object? param)
        {
            bool isGroup;
            switch (param)
            {
                case TreeNode node:
                    isGroup = node.Tag.IsGroup;
                    break;
                case CheckableTreeNode<Tag> checkableNode:
                    isGroup = checkableNode?.Item.IsGroup ?? false;
                    break;
                case Tag tag:
                    isGroup = tag.IsGroup;
                    break;
                default:
                    throw new Exception($"{nameof(TagTreeDataTemplateSelector)} does not support param of type {param?.GetType().Name}");
            }

            string key = isGroup ? "GroupTag" : "RegularTag";

            return AvailableTemplates[key];
        }
    }
}
