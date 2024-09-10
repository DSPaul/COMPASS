using COMPASS.Common.Tools;
using System.Collections.Generic;
using System.Linq;

namespace COMPASS.Common.Models.CodexProperties
{
    public class EnumerableProperty<T> : CodexProperty<IEnumerable<T>>
    {
        public EnumerableProperty(string propName, string? label = null) :
            base(propName, label)
        { }

        public override bool IsEmpty(IHasCodexMetadata codex)
        {
            IEnumerable<T>? value = GetProp(codex);

            return !value.SafeAny();
        }

        public override bool HasNewValue(IHasCodexMetadata toEvaluate, IHasCodexMetadata reference)
        {
            var newVal = GetProp(toEvaluate);
            if (!newVal.SafeAny())
            {
                //new value is nothing, don't consider it new data
                return false;
            }

            var refVal = GetProp(reference);
            if (refVal == null)
            {
                //existing is nothing, so anything is new compared to that
                return true;
            }

            return !newVal!.SequenceEqual(refVal);
        }
    }
}
