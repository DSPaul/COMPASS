using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace COMPASS.Models
{

    /// <summary>
    /// Class that contains all the global preferences, ready to be serialized
    /// </summary>
    [XmlRoot("SerializablePreferences")]
    public class GlobalPreferences
    {
        public List<int>? OpenFilePriorityIDs { get; set; }
        public List<CodexProperty> CodexProperties { get; set; } = new();

        public void CompleteLoading()
        {
            //In versions 1.6.0 and lower, label was stored instead of name
            var useLabel = CodexProperties.All(prop => string.IsNullOrEmpty(prop.Name) && !string.IsNullOrEmpty(prop.Label));
            if (useLabel)
            {
                for (int i = 0; i < CodexProperties.Count; i++)
                {
                    CodexProperty prop = CodexProperties[i];
                    var foundProp = Codex.Properties.Find(p => p.Label == prop.Label);
                    if (foundProp != null)
                    {
                        foundProp.OverwriteMode = prop.OverwriteMode;
                        CodexProperties[i] = foundProp;
                    }
                }
            }


            foreach (var defaultProp in Codex.Properties)
            {
                CodexProperty? prop = CodexProperties.Find(p => p.Name == defaultProp.Name);
                // Add Preferences from defaults if they weren't found on the loaded Preferences
                if (prop is null)
                {
                    prop = defaultProp;
                    CodexProperties.Add(prop);
                }
                prop.UpdateSources();
            }
        }

        public void PrepareForSave()
        {
            foreach (CodexProperty prop in CodexProperties)
            {
                prop.PrepareForSave();
            }
        }
    }
}
