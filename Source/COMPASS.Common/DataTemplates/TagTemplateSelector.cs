using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Metadata;
using COMPASS.Common.Models;
using System.Collections.Generic;

namespace COMPASS.Common.DataTemplates
{

    //based on https://github.com/AvaloniaUI/Avalonia.Samples/tree/main/src/Avalonia.Samples/DataTemplates/IDataTemplateSample

    public class TagTemplateSelector : ITreeDataTemplate
    {
        // This Dictionary should store our templates. We mark this as [Content], so we can directly add elements to it later.
        [Content]
        public Dictionary<string, ITreeDataTemplate> AvailableTemplates { get; } = new Dictionary<string, ITreeDataTemplate>();

        public Control? Build(object? param) => GetTemplate(param).Build(param);
        public InstancedBinding? ItemsSelector(object item) => GetTemplate(item).ItemsSelector(item);

        // Check if we can accept the provided data
        public bool Match(object? data) => data is TreeViewNode || data is CheckableTreeNode<Tag>;

        private ITreeDataTemplate GetTemplate(object? param)
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

            string key = isGroup ? "GroupTag" : "RegularTag";

            return AvailableTemplates[key];
        }
    }
}
