using System.Collections.Generic;
using System.Xml.Serialization;

namespace COMPASS.Common.Models.XmlDtos
{
    [XmlRoot("Tag"), XmlType("Tag")]
    public class TagDto
    {
        public int ID { get; set; } = -1;

        public string Content { get; set; } = "";

        [XmlElement("SerializableBackgroundColor")] //Backwards compatibility
        public ColorDto? BackgroundColor;

        public bool IsGroup { get; set; }
        public List<TagDto> Children { get; set; } = [];
        
        public List<string> LinkedGlobs { get; set; } = [];
    }
}
