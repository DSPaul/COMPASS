using COMPASS.Common.Tools;
using System;

namespace COMPASS.Common.Models
{
    public class SatchelInfo
    {
        public SatchelInfo()
        {
            CreationVersion = Reflection.Version;
            CreationDate = DateTime.Now;
        }

        /// <summary>
        /// Version of Compass used to create the satchel
        /// </summary>
        public string CreationVersion { get; init; }

        /// <summary>
        /// Date when the satchel was created
        /// </summary>
        public DateTime CreationDate { get; init; }

        /// <summary>
        /// Minimum version required to read the codexInfo file
        /// </summary>
        public string MinCodexInfoVersion { get; set; } = "1.7.0";

        /// <summary>
        /// Minimum version required to read the tags file
        /// </summary>
        public string MinTagsVersion { get; set; } = "1.7.0";

        /// <summary>
        /// Minimum version required to read the collectionInfo file
        /// </summary>
        public string MinCollectionInfoVersion { get; set; } = "1.7.0";
    }
}
