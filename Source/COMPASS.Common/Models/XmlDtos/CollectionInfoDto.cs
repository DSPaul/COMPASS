using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Serialization;
using COMPASS.Common.Tools;

namespace COMPASS.Common.Models.XmlDtos;

[XmlRoot("CollectionInfo"), XmlType("CollectionInfo")]
public class CollectionInfoDto
{
        #region Folders to Auto Import

        [Obsolete("Replaced by AutoImportFolders")]
        public List<string>? AutoImportDirectories { get; set; } = [];
        
        public List<FolderDto> AutoImportFolders { get; set; } = [];
        public List<string> BanishedPaths { get; set; } = [];
        #endregion
        
        [XmlArrayItem(ElementName = "FileType")]
        public List<KeyValuePairDto<string, bool>> FiletypePreferences { get; set; } = []; //because Dictionaries cannot be serialized
        
        [Obsolete("Stored on Tag")]
        public ObservableCollection<FolderTagPairDto> FolderTagPairs { get; set; } = [];
        
}