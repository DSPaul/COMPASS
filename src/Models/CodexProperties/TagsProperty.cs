using COMPASS.Models.XmlDtos;
using COMPASS.Tools;
using System;
using System.Linq;

namespace COMPASS.Models.CodexProperties
{
    public class TagsProperty : EnumerableProperty<Tag>
    {
        public TagsProperty(string propName, string? label = null) :
            base(propName, label)
        { }

        public override void SetProp(IHasCodexMetadata target, IHasCodexMetadata source)
        {
            if (source is Codex sourceCodex && target is Codex targetCodex)
            {
                foreach (var tag in sourceCodex.Tags.ToList())
                {
                    targetCodex.Tags.AddIfMissing(tag);
                }
            }

            else if (source is CodexDto sourceDto && target is CodexDto targetDto)
            {
                foreach (var tag in sourceDto.TagIDs.ToList())
                {
                    targetDto.TagIDs.AddIfMissing(tag);
                }
            }

            else
            {
                throw new InvalidOperationException("Target and source must be of same type");
            }

        }

        public override bool HasNewValue(IHasCodexMetadata toEvaluate, IHasCodexMetadata reference)
        {
            if (toEvaluate is Codex toEvalCodex && reference is Codex referenceCodex)
            {
                return toEvalCodex.Tags.Except(referenceCodex.Tags).Any();
            }

            if (toEvaluate is CodexDto toEvaluateDto && reference is CodexDto referenceDto)
            {
                return toEvaluateDto.TagIDs.Except(referenceDto.TagIDs).Any();
            }
            //TODO check which combination of Codex and CodexDto occur and need to be handled
            throw new InvalidOperationException("Target and source must be of same type");
        }
    }
}
