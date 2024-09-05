namespace COMPASS.Models.CodexProperties
{
    public class StringProperty : CodexProperty<string?>
    {
        public StringProperty(string propName, string? label = null) :
            base(propName, label)
        { }

        public override bool IsEmpty(Codex codex) => string.IsNullOrEmpty(GetProp(codex));
    }
}
