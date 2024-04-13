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
            FiletypePreferences.Count > 0 ||
            FolderTagPairs.Count > 0;

        #region Folders to Auto Import

        [Obsolete("Replaced by AutoImportFolders")]
        public ObservableCollection<string>? AutoImportDirectories { get; set; } = new();
        public ObservableCollection<Folder> AutoImportFolders { get; set; } = new();

        public ObservableCollection<string> BanishedPaths { get; set; } = new();
        #endregion

        #region File Type preferences for auto import
        private Dictionary<string, bool>? _filetypePreferences;
        [XmlIgnore]
        public Dictionary<string, bool> FiletypePreferences
        {
            get => _filetypePreferences ??= SerializableFiletypePreferences.ToDictionary(x => x.Key, x => x.Value);
            set => _filetypePreferences = value;
        }

        [XmlArray(ElementName = "FiletypePreferences")]
        [XmlArrayItem(ElementName = "FileType")]
        public List<ObservableKeyValuePair<string, bool>> SerializableFiletypePreferences { get; set; } = new(); //needed because Dictionaries cannot be serialized

        //Copies the dict into the serializable version
        public void PrepareSave() => SerializableFiletypePreferences = FiletypePreferences.Select(x => new ObservableKeyValuePair<string, bool>(x)).ToList();

        #endregion

        #region Folder -> Tag mapping
        public ObservableCollection<FolderTagPair> FolderTagPairs { get; set; } = new();

        public void CompleteLoading(CodexCollection owner)
        {
            //Set the collections on all the tags
            foreach (var pair in FolderTagPairs)
            {
                pair.InitTag(owner);
            }

            #region Migrate save data from previous versions

#pragma warning disable CS0618 // Type or member is obsolete

            //(1.6.0 -> 1.7.0) migrate from AutoImportFoldersViewSource to AutoImportFolders 
            if (AutoImportDirectories is not null && AutoImportDirectories.Any() && !AutoImportFolders.Any())
            {
                AutoImportFolders = new(AutoImportDirectories.Select(dir => new Folder(dir)));
                AutoImportDirectories = null;
            }

#pragma warning restore CS0618 // Type or member is obsolete

            #endregion
        }

        public void MergeWith(CollectionInfo other)
        {
            AutoImportFolders.AddRange(other.AutoImportFolders);
            BanishedPaths.AddRange(other.BanishedPaths);
            //For file type prefs, overwrite if already in dict, add otherwise
            other.PrepareSave(); // make sure serializable prefs are in sync in prefs in other
            foreach (var pref in other.SerializableFiletypePreferences)
            {
                var existing = SerializableFiletypePreferences.Find(x => x.Key == pref.Key);
                if (existing != default)
                {
                    existing.Value = pref.Value;
                }
                else
                {
                    SerializableFiletypePreferences.Add(pref);
                }
            }
            _filetypePreferences = null; //reset lazy loading
            FolderTagPairs.AddRange(other.FolderTagPairs);

        }
        #endregion
    }
}
