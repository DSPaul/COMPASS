using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace COMPASS.Common.Models.XmlDtos
{
    [XmlRoot("Codex"), XmlType("Codex")]
    public class CodexDto : IHasCodexMetadata
    {

        #region COMPASS related Metadata
        public int ID { get; set; }

        [XmlElement("CoverArt")]//Backwards compatibility
        public string CoverArtPath { get; set; } = "";

        [XmlElement("Thumbnail")]//Backwards compatibility
        public string ThumbnailPath { get; set; } = "";
        #endregion

        #region Codex related Metadata

        public string Title { get; set; } = "";
        [XmlElement("SerializableSortingTitle")] //Backwards compatibility
        public string SortingTitle { get; set; } = "";
        public List<string> Authors { get; set; } = [];
        public string Publisher { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime? ReleaseDate { get; set; }
        public int PageCount { get; set; }
        public string Version { get; set; } = "";

        #endregion

        #region User related Metadata
        public List<int> TagIDs { get; set; } = [];
        public bool PhysicallyOwned { get; set; }
        public int Rating { get; set; }
        public bool Favorite { get; set; }

        #endregion

        #region User behaviour metadata
        public DateTime DateAdded { get; set; } = DateTime.Now;
        public DateTime LastOpened { get; set; }
        public int OpenedCount { get; set; }
        #endregion

        #region Sources
        public string SourceURL { get; set; } = "";
        public string Path { get; set; } = "";
        public string ISBN { get; set; } = "";

        #endregion

    }
}
