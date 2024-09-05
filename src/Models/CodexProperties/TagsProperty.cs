using COMPASS.Tools;
using System.Linq;

namespace COMPASS.Models.CodexProperties
{
    public class TagsProperty : EnumerableProperty<Tag>
    {
        public TagsProperty(string propName, string? label = null) :
            base(propName, label)
        { }

        public override void SetProp(Codex target, Codex source)
        {
            foreach (var tag in source.Tags)
            {
                App.SafeDispatcher.Invoke(() => target.Tags.AddIfMissing(tag));
            }
        }

        public override bool HasNewValue(Codex toEvaluate, Codex reference) => toEvaluate.Tags.Except(reference.Tags).Any();
    }
}
