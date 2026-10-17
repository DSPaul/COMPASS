
namespace COMPASS.Common.Features.Metadata.Properties
{
    public class StringProperty : CodexProperty<string?>
    {
        public StringProperty(string propName, string? label = null) :
            base(propName, label)
        { }

        public override bool IsEmpty(IHasCodexMetadata codex) => string.IsNullOrEmpty(GetProp(codex));
    }
}
