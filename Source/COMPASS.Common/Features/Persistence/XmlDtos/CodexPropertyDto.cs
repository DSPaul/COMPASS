using COMPASS.Common.Features.Metadata;
using System.Xml.Serialization;

namespace COMPASS.Common.Features.Persistence.XmlDtos
{
    [XmlRoot("CodexProperty")]
    public class CodexPropertyDto
    {
        public string Name { get; set; } = string.Empty;

        [Obsolete("Label is now determined based on the Name")]
        public string Label { get; set; } = string.Empty;


        #region Import Sources

        //Use to be called ...MetaData so we are stuck with that spelling for backwards compatibility
        [XmlArrayItem(ElementName = "MetaDataSourceType")]
        public List<MetadataSourceType> SourcePriority { get; set; } = [];

        public MetadataOverwriteMode OverwriteMode { get; set; } = MetadataOverwriteMode.IfEmpty;


        #endregion
    }
}
