using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Metadata;
using COMPASS.Common.Models;

namespace COMPASS.Common.DataTemplates
{

    //based on https://github.com/AvaloniaUI/Avalonia.Samples/tree/main/src/Avalonia.Samples/DataTemplates/IDataTemplateSample

    public class TagDataTemplateSelector : IDataTemplate
    {
        // This Dictionary should store our templates. We mark this as [Content], so we can directly add elements to it later.
        [Content]
        public Dictionary<string, IDataTemplate> AvailableTemplates { get; } = [];

        public Control? Build(object? param) => GetTemplate(param).Build(param);

        // Check if we can accept the provided data
        public bool Match(object? data) => data is Tag;

        private IDataTemplate GetTemplate(object? param)
        {
            bool isGroup;
            switch (param)
            {
                case Tag tag:
                    isGroup = tag.IsGroup;
                    break;
                default:
                    throw new Exception($"{nameof(TagDataTemplateSelector)} does not support param of type {param?.GetType().Name}");
            }

            string key = isGroup ? "GroupTag" : "RegularTag";

            return AvailableTemplates[key];
        }
    }
}
