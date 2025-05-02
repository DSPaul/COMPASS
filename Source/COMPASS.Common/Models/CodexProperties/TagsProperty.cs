﻿using System;
using System.Collections.Generic;
using System.Linq;
using COMPASS.Common.Models.XmlDtos;
using COMPASS.Common.Tools;

namespace COMPASS.Common.Models.CodexProperties
{
    public class TagsProperty : EnumerableProperty<int> //use for all operations except the set, so int type
    {
        public TagsProperty(string propName, string? label = null) :
            base(propName, label)
        { }

        protected override IEnumerable<int>? GetProp(IHasCodexMetadata codex)
        {
            return codex switch
            {
                Codex c => c.ToDto().TagIDs,
                CodexDto dto => dto.TagIDs,
                _ => throw new InvalidOperationException("Unknown implementation of IHasCodexMetadata")
            };
        }

        public override void SetProp(IHasCodexMetadata target, IHasCodexMetadata source)
        {
            if (source is Codex sourceCodex && target is Codex targetCodex)
            {
                foreach (Tag tag in sourceCodex.Tags.ToList())
                {
                    targetCodex.Tags.AddIfMissing(tag);
                }
            }

            else if (source is CodexDto sourceDto && target is CodexDto targetDto)
            {
                foreach (var id in sourceDto.TagIDs.ToList())
                {
                    targetDto.TagIDs.AddIfMissing(id);
                }
            }

            else
            {
                throw new InvalidOperationException("Target and source must be of same type");
            }

        }

        public override bool HasNewValue(IHasCodexMetadata toEvaluate, IHasCodexMetadata reference) =>
            GetProp(toEvaluate)!.Except(GetProp(reference)!).Any();
    }
}
