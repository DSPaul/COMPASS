using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using COMPASS.Common.Models;
using System.Collections.Generic;

namespace COMPASS.Common.Tools
{

    //based on https://github.com/AvaloniaUI/Avalonia.Samples/tree/main/src/Avalonia.Samples/DataTemplates/IDataTemplateSample

    public class TagTemplateSelector : IDataTemplate
    {
        // This Dictionary should store our templates. We mark this as [Content], so we can directly add elements to it later.
        [Content]
        public Dictionary<string, IDataTemplate> AvailableTemplates { get; } = new Dictionary<string, IDataTemplate>();

        public Control? Build(object? param) //TODO: check that the param is actually the tag
        {
            bool isGroup;
            if (param is TreeViewNode node)
            {
                isGroup = node.Tag.IsGroup;
            }
            else
            {
                CheckableTreeNode<Tag>? checkNode = param as CheckableTreeNode<Tag>;
                isGroup = checkNode?.Item.IsGroup ?? false;
            }

            if (isGroup)
            {
                return AvailableTemplates["GroupTag"].Build(param);
            }
            else
            {
                return AvailableTemplates["RegularTag"].Build(param);
            }
        }

        // Check if we can accept the provided data
        public bool Match(object? data) //TODO: check that the data is actually the tag
        {
            return data is TreeViewNode || data is CheckableTreeNode<Tag>;
        } 
    }
}
