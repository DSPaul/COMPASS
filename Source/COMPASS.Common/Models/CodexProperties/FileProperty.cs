using System.IO;

namespace COMPASS.Common.Models.CodexProperties
{
    public class FileProperty : StringProperty
    {
        public FileProperty(string propName, string? label = null) :
            base(propName, label)
        { }

        public override bool IsEmpty(IHasCodexMetadata codex) => !File.Exists(GetProp(codex));
    }
}
