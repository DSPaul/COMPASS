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
        public List<int> OpenFilePriorityIDs { get; set; }
        public List<CodexProperty> CodexProperties { get; set; } = new();

        public void Init()
        {
            foreach (var DefaultProp in Codex.Properties)
            {
                CodexProperty prop = CodexProperties.FirstOrDefault(p => p.Label == DefaultProp.Label);
                // Add Preferences from defaults if they weren't found on the loaded Preferences
                if (prop is null)
                {
                    prop = DefaultProp;
                    CodexProperties.Add(prop);
                }
                //Call init on every preference
                prop.UpdateSources();
            }
        }
    }
}
