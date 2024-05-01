using COMPASS.Models.Preferences;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace COMPASS.Models.XmlDtos
{

    /// <summary>
    /// Class that contains all the global preferences, ready to be serialized
    /// </summary>
    [XmlRoot("SerializablePreferences")]
    public class PreferencesDto
    {
        public List<int>? OpenFilePriorityIDs { get; set; }

        [XmlArray(ElementName = "CodexProperties")]
        [XmlArrayItem(ElementName = "CodexProperty")]
        public List<CodexPropertyDto> CodexProperties { get; set; } = new();


        public ListLayoutPreferences ListLayoutPreferences { get; set; } = new();
        public CardLayoutPreferences CardLayoutPreferences { get; set; } = new();
        public TileLayoutPreferences TileLayoutPreferences { get; set; } = new();
        public HomeLayoutPreferences HomeLayoutPreferences { get; set; } = new();

        public UIState UIState { get; set; } = new();

        public bool AutoLinkFolderTagSameName { get; set; } = true;
    }
}
