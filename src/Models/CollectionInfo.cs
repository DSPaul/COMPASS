using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Serialization;

namespace COMPASS.Models
{
    /// <summary>
    /// Contains all the Info on a Collection that needs to be serialized
    /// </summary>
    public class CollectionInfo
    {
        /// <summary>
        /// Checks if the collections has settings that differ from the default settings
        /// </summary>
        /// <returns>True if settings were moddified, false if they are default</returns>
        public bool ContainsSettings() =>
            AutoImportDirectories.Count > 0 ||
            BanishedPaths.Count > 0 ||
            FiletypePreferences.Count > 0 ||
            FolderTagPairs.Count > 0;

        #region Folders to Auto Import
        //Folders to check for new files
        public ObservableCollection<string> AutoImportDirectories { get; set; } = new();
        public ObservableCollection<string> BanishedPaths { get; set; } = new();
        #endregion

        #region File Type preferences for auto import
        private Dictionary<string, bool> _filetypePreferences;
        [XmlIgnore]
        public Dictionary<string, bool> FiletypePreferences
        {
            get => _filetypePreferences ??= SerializableFiletypePreferences.ToDictionary(x => x.Key, x => x.Value);
            set => _filetypePreferences = value;
        }

        [XmlArray(ElementName = "FiletypePreferences")]
        [XmlArrayItem(ElementName = "FileType")]
        public List<ObservableKeyValuePair<string, bool>> SerializableFiletypePreferences { get; set; } = new(); //needed because Dicts cannot be serialized

        //Copies the dict into the serializable version
        public void PrepareSave() => SerializableFiletypePreferences = FiletypePreferences.Select(x => new ObservableKeyValuePair<string, bool>(x)).ToList();

        #endregion

        #region Folder -> Tag mapping
        public ObservableCollection<FolderTagPair> FolderTagPairs { get; set; } = new();

        public void CompleteLoading(CodexCollection owner)
        {
            foreach (var pair in FolderTagPairs)
            {
                pair.InitTag(owner);
            }
        }
        #endregion
    }
}
