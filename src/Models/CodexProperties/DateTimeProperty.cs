using System;

namespace COMPASS.Models.CodexProperties
{
    public class DateTimeProperty : CodexProperty<DateTime?>
    {
        public DateTimeProperty(string propName, string? label = null) :
            base(propName, label)
        { }

        public override bool IsEmpty(IHasCodexMetadata codex)
        {
            DateTime? value = GetProp(codex);
            return value is null || value == DateTime.MinValue;
        }
    }
}
