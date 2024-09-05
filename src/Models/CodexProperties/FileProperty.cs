using System.IO;

namespace COMPASS.Models.CodexProperties
{
    public class FileProperty : StringProperty
    {
        public FileProperty(string propName, string? label = null) :
            base(propName, label)
        { }

        public override bool IsEmpty(Codex codex) => !File.Exists(GetProp(codex));
    }
}
