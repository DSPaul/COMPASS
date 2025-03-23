using COMPASS.Common.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Serialization;

namespace COMPASS.Common.Models
{
    /// <summary>
    /// Contains all the Info on a Collection that needs to be serialized
    /// </summary>
    public class CollectionInfo
    {
        /// <summary>
        /// Checks if the collections has settings that differ from the default settings
        /// </summary>
        /// <returns>True if settings were modified, false if they are default</returns>
        public bool ContainsSettings() =>
            AutoImportFolders.Count > 0 ||
            BanishedPaths.Count > 0 ||
            FiletypePreferences.Count > 0;

        #region Folders to Auto Import
        
        public ObservableCollection<Folder> AutoImportFolders { get; set; } = [];

        public ObservableCollection<string> BanishedPaths { get; set; } = [];
        #endregion

        #region File Type preferences for auto import

        public Dictionary<string, bool> FiletypePreferences { get; set; } = [];
        
        #endregion

        public void MergeWith(CollectionInfo other)
        {
            AutoImportFolders.AddRange(other.AutoImportFolders);
            BanishedPaths.AddRange(other.BanishedPaths);
            
            //For file type prefs, overwrite if already in dict, add otherwise
            foreach (var pref in FiletypePreferences)
            {
                other.FiletypePreferences[pref.Key] = pref.Value;
            }
        }
    }
}
