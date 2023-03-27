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
        public List<int> OpenFilePriorityIDs { get; set; }
    }
}
