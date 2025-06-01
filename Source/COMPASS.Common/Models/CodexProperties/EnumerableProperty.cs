using System.Collections.Generic;
using System.Linq;
using COMPASS.Common.Tools;

namespace COMPASS.Common.Models.CodexProperties
{
    public class EnumerableProperty<T> : CodexProperty<IList<T>>
    {
        public EnumerableProperty(string propName, string? label = null) :
            base(propName, label)
        { }

        public override bool IsEmpty(IHasCodexMetadata codex)
        {
            IList<T>? value = GetProp(codex);

            return !value.SafeAny();
        }

        public override bool HasNewValue(SourceMetaData toEvaluate, Codex reference)
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

        public override void Copy(SourceMetaData source, SourceMetaData target)
        {
            target.SetProperty(Name, GetProp(source)?.ToList() ?? []); //make a new list
        }
        
        public override void Apply(SourceMetaData source, Codex codex)
        {
            codex.SetProperty(Name, GetProp(source)?.ToList() ?? []); //make a new list
        }
    }
}
