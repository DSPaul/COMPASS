using System.Collections.Generic;
using System.Linq;
using COMPASS.Common.Tools;

namespace COMPASS.Common.Models.CodexProperties
{
    public class TagsProperty : EnumerableProperty<Tag>
    {
        public TagsProperty(string propName, string? label = null) :
            base(propName, label)
        { }
        
        public override void Apply(SourceMetaData source, Codex target)
        {
            //Tags received from a metadata source should add tags to the existing ones, rather than overwrite the existing list
            foreach (Tag tag in source.Tags)
            {
                target.Tags.AddIfMissing(tag);
            }
        }

        public override bool HasNewValue(SourceMetaData toEvaluate, Codex reference) =>
            GetProp(toEvaluate)!.Except(GetProp(reference)!).Any();
    }
}
