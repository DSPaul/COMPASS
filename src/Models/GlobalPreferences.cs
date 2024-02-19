using System.Collections.Generic;
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

        public void Init()
        {
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
    }
}
