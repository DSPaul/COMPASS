using System.Numerics;

namespace COMPASS.Models.CodexProperties
{
    public class NumberProperty<T> : CodexProperty<T> where T : INumber<T>
    {
        public NumberProperty(string propName, string? label = null) :
            base(propName, label)
        { }

        public override bool IsEmpty(Codex codex) => T.IsZero(GetProp(codex)!);
    }
}
