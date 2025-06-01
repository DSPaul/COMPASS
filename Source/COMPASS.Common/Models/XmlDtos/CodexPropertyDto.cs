using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using COMPASS.Common.Models.Enums;

namespace COMPASS.Common.Models.XmlDtos
{
    [XmlRoot("CodexProperty")]
    public class CodexPropertyDto
    {
        public string Name { get; set; } = string.Empty;

        [Obsolete("Label is now determined based on the Name")]
        public string Label { get; set; } = string.Empty;


        #region Import Sources

        public List<MetaDataSourceType> SourcePriority { get; set; } = [];

        public MetaDataOverwriteMode OverwriteMode { get; set; } = MetaDataOverwriteMode.IfEmpty;


        #endregion
    }
}
